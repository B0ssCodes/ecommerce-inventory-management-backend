using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using Microsoft.Net.Http.Headers;

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

        if (action == "login")
        {
            await _next(httpContext);
            return;
        }
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

            if (model == "product" && action == "create")
            {
                beforeStateJson = JsonSerializer.Serialize(new ProductRequestDTO());
            }
            else if (model == "product" && action == "update")
            {
                id = int.Parse(pathSegments[3]);
                beforeStateJson = JsonSerializer.Serialize(await productRepository.GetProduct(id));
            }
            else if (model == "analytics")
            {
                beforeStateJson = JsonSerializer.Serialize(new { analytics = $"{action}" });
            }
            else if ((action == "create" || action == "submit") && model != "product")
            {
                switch (model)
                {
                    case "category":
                        beforeStateJson = JsonSerializer.Serialize(new CategoryRequestDTO());
                        break;
                    case "transaction":
                        beforeStateJson = JsonSerializer.Serialize(new TransactionCreateDTO());
                        break;
                    case "userRole":
                        beforeStateJson = JsonSerializer.Serialize(new UserRoleRequestDTO());
                        break;
                    case "vendor":
                        beforeStateJson = JsonSerializer.Serialize(new VendorRequestDTO());
                        break;
                    default:
                        beforeStateJson = JsonSerializer.Serialize(new { item = "created" });
                        break;
                }

                using (var stream = new StreamReader(httpContext.Request.Body, Encoding.UTF8, leaveOpen: true))
                {
                    afterStateJson = await stream.ReadToEndAsync();
                    httpContext.Request.Body.Position = 0;
                }
            }
            else if ((action == "update" && model != "product"))
            {
                id = int.Parse(pathSegments[3]);
                switch (model)
                {
                    case "category":
                        beforeStateJson = JsonSerializer.Serialize(await categoryRepository.GetCategory(id));
                        break;
                    case "user":
                        beforeStateJson = JsonSerializer.Serialize(await userRepository.GetUser(id));
                        break;
                    case "userRole":
                        beforeStateJson = JsonSerializer.Serialize(await userRoleRepository.GetUserRole(id));
                        break;
                    case "vendor":
                        beforeStateJson = JsonSerializer.Serialize(await vendorRepository.GetVendor(id));
                        break;
                    default:
                        beforeStateJson = JsonSerializer.Serialize(new { item = "updated" });
                        break;
                }

                using (var stream = new StreamReader(httpContext.Request.Body, Encoding.UTF8, leaveOpen: true))
                {
                    afterStateJson = await stream.ReadToEndAsync();
                    httpContext.Request.Body.Position = 0;
                }
            }
            else if (action == "delete")
            {
                id = int.Parse(pathSegments[3]);
                switch (model)
                {
                    case "category":
                        beforeStateJson = JsonSerializer.Serialize(await categoryRepository.GetCategory(id));
                        break;
                    case "user":
                        beforeStateJson = JsonSerializer.Serialize(await userRepository.GetUser(id));
                        break;
                    case "userRole":
                        beforeStateJson = JsonSerializer.Serialize(await userRoleRepository.GetUserRole(id));
                        break;
                    case "vendor":
                        beforeStateJson = JsonSerializer.Serialize(await vendorRepository.GetVendor(id));
                        break;
                    default:
                        beforeStateJson = JsonSerializer.Serialize(new { item = "updated" });
                        break;
                }
            }
            else if (action.StartsWith("get", StringComparison.OrdinalIgnoreCase) && pathSegments.Length > 3)
            {
                if (int.TryParse(pathSegments[3], out id) && id != 0)
                {
                    beforeStateJson = JsonSerializer.Serialize(new { requested = $"{model} with id {id}" });
                }
            }
            else
            {
                using (var stream = new StreamReader(httpContext.Request.Body, Encoding.UTF8, leaveOpen: true))
                {
                    beforeStateJson = await stream.ReadToEndAsync();
                    httpContext.Request.Body.Position = 0;
                }
            }

            int userId = 0;
            string token = httpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (!string.IsNullOrWhiteSpace(token))
            {
                // Get the user ID from the passed JWT Token
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                string userIdString = jwtToken.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;

                // Convert userId to integer
                if (!int.TryParse(userIdString, out userId))
                {
                    _logger.LogError("Invalid User ID: {UserIdString}", userIdString);
                }
            }

            // Get the response body
            var originalResponseBodyStream = httpContext.Response.Body;
            using (var responseBodyStream = new MemoryStream())
            {
                httpContext.Response.Body = responseBodyStream;

                try
                {
                    // Process the request then get the response body
                    await _next(httpContext);

                    responseBodyStream.Seek(0, SeekOrigin.Begin);
                    var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                    responseBodyStream.Seek(0, SeekOrigin.Begin);

                    if (action != "create" && action != "update")
                    {
                        afterStateJson = responseBody;
                    }

                    string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string logName = $"{model} {action} by {userId} : {dateTime} ";
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
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing the request.");
                    throw;
                }
                finally
                {
                    httpContext.Response.Body = originalResponseBodyStream;
                }
            }
        }
    }
}