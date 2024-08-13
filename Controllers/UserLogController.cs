using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Inventory_Management_Backend.Controllers
{
    [Route("api/userLog")]
    [ApiController]
    public class UserLogController : ControllerBase
    {
        private readonly IUserLogRepository _userLogRepository;
        private readonly ApiResponse _response;

        public UserLogController(IUserLogRepository userLogRepository, ApiResponse response)
        {
            _userLogRepository = userLogRepository;
            _response = response;
        }

        [HttpPost]
        [Route("get")]
        public async Task<IActionResult> GetUserLogs(PaginationParams paginatioParams)
        {
            try
            {
                var (result, itemCount) = await _userLogRepository.GetUserLogs(paginatioParams);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "User logs retrieved successfully";
                _response.Result = result;
                _response.ItemCount = itemCount;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = true;
                _response.Message = ex.Message;
                _response.Result = default;
                _response.ItemCount = default;
                return BadRequest(_response);
            }
            
        }

        [HttpGet]
        [Route("get/{logID}")]
        public async Task<IActionResult> GetUserLog(int logID)
        {
            try
            {
                UserLogResponseDTO result = await _userLogRepository.GetUserLog(logID);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "User log retrieved successfully";
                _response.Result = result;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = true;
                _response.Message = ex.Message;
                _response.Result = default;
                return BadRequest(_response);
            }
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateUserLog(UserLogRequestDTO requestDTO)
        {
            try
            {
                await _userLogRepository.CreateUserLog(requestDTO);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "User log created successfully";
                _response.Result = default;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = true;
                _response.Message = ex.Message;
                _response.Result = default;
                return BadRequest(_response);
            }
        }
    }
}
