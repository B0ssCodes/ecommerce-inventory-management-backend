using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Inventory_Management_Backend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepository;
        private readonly ApiResponse _apiResponse;

        public AuthController(IAuthRepository authRepository, ApiResponse apiResponse)
        {
            _authRepository = authRepository;
            _apiResponse = apiResponse;
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register(RegisterRequestDTO registerDTO)
        {
            try
            {
                await _authRepository.Register(registerDTO);
                _apiResponse.StatusCode = HttpStatusCode.OK;
                _apiResponse.IsSuccess = true;
                _apiResponse.Message = "User registered successfully";
                _apiResponse.Result = null;
                return Ok(_apiResponse);

            }
            catch(Exception ex)
            {
                _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                _apiResponse.IsSuccess = false;
                _apiResponse.Message = ex.Message;
                _apiResponse.Result = null;
                return BadRequest(_apiResponse);
            }
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login(LoginRequestDTO loginDTO)
        {
            try
            {
                LoginResponseDTO loginResponse = await _authRepository.Login(loginDTO);
                _apiResponse.StatusCode = HttpStatusCode.OK;
                _apiResponse.IsSuccess = true;
                _apiResponse.Message = "User logged in successfully";
                _apiResponse.Result = loginResponse;
                return Ok(_apiResponse);
            }
            catch (Exception ex)
            {
                _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                _apiResponse.IsSuccess = false;
                _apiResponse.Message = ex.Message;
                _apiResponse.Result = null;
                return BadRequest(_apiResponse);
            }
        }

    }
}
