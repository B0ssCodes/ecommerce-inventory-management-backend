using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Inventory_Management_Backend.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApiResponse _response;
        private readonly IUserRepository _userRepository;

        public UserController(IUserRepository userRepository, ApiResponse response)
        {
            _userRepository = userRepository;
            _response = response;
        }

        [HttpPost]
        //[Authorize]
        [Route("get")]
        public async Task<IActionResult> GetUsers(PaginationParams paginationParams)
        {
            try
            {
                var (userResponseDTOs, itemCount) = await _userRepository.GetUsers(paginationParams);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Users retrieved successfully";
                _response.Result = userResponseDTOs;
                _response.ItemCount = itemCount;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                _response.Result = default;
                return BadRequest(_response);

            }

        }

        [HttpGet]
        //[Authorize]
        [Route("get/{userID}")]
        public async Task<IActionResult> GetUser(int userID)
        {
            try
            {
                UserResponseDTO userResponseDTO = await _userRepository.GetUser(userID);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "User retrieved successfully";
                _response.Result = userResponseDTO;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                _response.Result = default;
                return BadRequest(_response);
            }
        }

        [HttpDelete]
        [Authorize]
        [Route("delete/{userID}")]
        public async Task<IActionResult> DeleteUser(int userID)
        {
            try
            {
                await _userRepository.DeleteUser(userID);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "User deleted successfully";
                _response.Result = default;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                _response.Result = default;
                return BadRequest(_response);
            }
        }


    }
}
