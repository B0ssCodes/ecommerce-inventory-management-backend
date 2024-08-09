using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text;

public class UserLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserLoggingMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    public UserLoggingMiddleware(RequestDelegate next, ILogger<UserLoggingMiddleware> logger, IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        httpContext.Request.EnableBuffering();

        var pathSegments = httpContext.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string action = pathSegments.Length > 2 ? pathSegments[2] : "Unknown";
        string model = pathSegments.Length > 1 ? pathSegments[1] : "Unknown";

        int id = 0;
        string requestJson = null;
        string beforeStateJson = null;
        string afterStateJson = null;

        using (var scope = _serviceProvider.CreateScope())
        {
            var userLogRepository = scope.ServiceProvider.GetRequiredService<IUserLogRepository>();
            var categoryRepository = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
            var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var userRoleRepository = scope.ServiceProvider.GetRequiredService<IUserRoleRepository>();
            var vendorRepository = scope.ServiceProvider.GetRequiredService<IVendorRepository>();

            if ((action == "update" || action == "create") && model == "product")
            {
                beforeStateJson = JsonSerializer.Serialize(new { product = "created" });

                // Read the request body
                using (var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, leaveOpen: true))
                {
                    requestJson = await reader.ReadToEndAsync();
                    _logger.LogInformation("Request Body: {Body}", requestJson);
                    httpContext.Request.Body.Position = 0;
                }
            }
            else if ((action == "update" || action == "delete") && pathSegments.Length > 3 && int.TryParse(pathSegments[3], out id))
            {
                if (model != "product")
                {
                    object beforeState = null;
                    switch (model)
                    {
                        case "category":
                            beforeState = await categoryRepository.GetCategory(id);
                            break;
                        case "user":
                            beforeState = await userRepository.GetUser(id);
                            break;
                        case "userRole":
                            beforeState = await userRoleRepository.GetUserRole(id);
                            break;
                        case "vendor":
                            beforeState = await vendorRepository.GetVendor(id);
                            break;
                    }

                    // Convert beforeState object to JSON string
                    beforeStateJson = JsonSerializer.Serialize(beforeState);

                    // Read the request body
                    using (var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, leaveOpen: true))
                    {
                        requestJson = await reader.ReadToEndAsync();
                        _logger.LogInformation("Request Body: {Body}", requestJson);
                        httpContext.Request.Body.Position = 0;
                    }

                    afterStateJson = requestJson;
                }
            }
            else
            {
                using (var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, leaveOpen: true))
                {
                    requestJson = await reader.ReadToEndAsync();
                    _logger.LogInformation("Request Body: {Body}", requestJson);
                    httpContext.Request.Body.Position = 0;
                }

                beforeStateJson = requestJson;
            }

            // Retrieve and log the Authorization header
            int userId = 0;
            string token = httpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (!string.IsNullOrWhiteSpace(token))
            {
                // Extract user ID from the token
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                string userIdString = jwtToken.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;

                // Convert userId to integer
                if (!int.TryParse(userIdString, out userId))
                {
                    _logger.LogError("Invalid User ID: {UserIdString}", userIdString);
                    // Handle the error as needed, e.g., return a response or throw an exception
                }
            }

            // Capture the response body
            var originalResponseBodyStream = httpContext.Response.Body;
            using (var responseBodyStream = new MemoryStream())
            {
                httpContext.Response.Body = responseBodyStream;

                // Process the request
                await _next(httpContext);

                // Read and log the response body
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                responseBodyStream.Seek(0, SeekOrigin.Begin);

                if (action == "update" || action == "create")
                {
                    afterStateJson = responseBody;
                }
                else if (action != "update" && action != "delete")
                {
                    afterStateJson = responseBody;
                }

                string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logName = $"{model} {action} by {userId} / {dateTime} ";
                UserLogRequestDTO requestDTO = new UserLogRequestDTO
                {
                    UserID = userId,
                    LogName = logName,
                    Action = action,
                    Model = model,
                    BeforeState = beforeStateJson,
                    AfterState = afterStateJson,
                };

                // Call the createUserLog method with request and response bodies
                await userLogRepository.CreateUserLog(requestDTO);

                // Copy the response body back to the original stream
                await responseBodyStream.CopyToAsync(originalResponseBodyStream);
            }
        }
    }
}