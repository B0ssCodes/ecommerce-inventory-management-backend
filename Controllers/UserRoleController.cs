using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Inventory_Management_Backend.Controllers
{
    [Route("api/userRole")]
    [ApiController]
    public class UserRoleController : ControllerBase 
    {
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly ApiResponse _apiResponse;

        public UserRoleController(IUserRoleRepository userRoleRepository, ApiResponse apiResponse)
        {
            _userRoleRepository = userRoleRepository;
            _apiResponse = apiResponse;
        }

        [HttpGet]
        [Route("getUserRoles")]
        public async Task<IActionResult> GetUserRoles()
        {
            try
            {
                List<UserRoleDTO> userRoles = await _userRoleRepository.GetUserRoles();
                _apiResponse.StatusCode = HttpStatusCode.OK;
                _apiResponse.IsSuccess = true;
                _apiResponse.Message = "User roles retrieved successfully";
                _apiResponse.Result = userRoles;
                return Ok(_apiResponse);
            }
            
            catch (Exception Ex)
            {
                _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                _apiResponse.IsSuccess = false;
                _apiResponse.Message = Ex.Message;
                _apiResponse.Result = null;
                return BadRequest(_apiResponse);
            }
        }

        [HttpGet]
        [Route("getUserRole/{userRoleId}")]
        public async Task<IActionResult> GetUserRole(int userRoleId)
        {
            try
            {
                UserRoleDTO userRole = await _userRoleRepository.GetUserRole(userRoleId);
                _apiResponse.StatusCode = HttpStatusCode.OK;
                _apiResponse.IsSuccess = true;
                _apiResponse.Message = "User role retrieved successfully";
                _apiResponse.Result = userRole;
                return Ok(_apiResponse);
            }
            
            catch (Exception Ex)
            {
                _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                _apiResponse.IsSuccess = false;
                _apiResponse.Message = Ex.Message;
                _apiResponse.Result = null;
                return BadRequest(_apiResponse);
            }
        }

        [HttpPost]
        [Route("createUserRole")]
        public async Task<IActionResult> CreateUserRole(string roleName)
        {
            try
            {
                UserRoleDTO userRole = await _userRoleRepository.CreateUserRole(roleName);
                _apiResponse.StatusCode = HttpStatusCode.OK;
                _apiResponse.IsSuccess = true;
                _apiResponse.Message = "User role created successfully";
                _apiResponse.Result = userRole;
                return Ok(_apiResponse);
            }
            
            catch (Exception Ex)
            {
                _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                _apiResponse.IsSuccess = false;
                _apiResponse.Message = Ex.Message;
                _apiResponse.Result = null;
                return BadRequest(_apiResponse);
            }
        }

        [HttpPut]
        [Route("updateUserRole/{roleId}")]
        public async Task<IActionResult> UpdateUserRole(int roleId, string roleName)
        {
            try
            {
                UserRoleDTO userRole = await _userRoleRepository.UpdateUserRole(roleId, roleName);
                _apiResponse.StatusCode = HttpStatusCode.OK;
                _apiResponse.IsSuccess = true;
                _apiResponse.Message = "User role updated successfully";
                _apiResponse.Result = userRole;
                return Ok(_apiResponse);
            }
            
            catch (Exception Ex)
            {
                _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                _apiResponse.IsSuccess = false;
                _apiResponse.Message = Ex.Message;
                _apiResponse.Result = null;
                return BadRequest(_apiResponse);
            }
        }

        [HttpDelete]
        [Route("deleteUserRole/{roleId}")]
        public async Task<IActionResult> DeleteUserRole(int roleId)
        {
            try
            {
                await _userRoleRepository.DeleteUserRole(roleId);
                _apiResponse.StatusCode = HttpStatusCode.OK;
                _apiResponse.IsSuccess = true;
                _apiResponse.Message = "User role deleted successfully";
                _apiResponse.Result = null;
                return Ok(_apiResponse);
            }
            
            catch (Exception Ex)
            {
                _apiResponse.StatusCode = HttpStatusCode.BadRequest;
                _apiResponse.IsSuccess = false;
                _apiResponse.Message = Ex.Message;
                _apiResponse.Result = null;
                return BadRequest(_apiResponse);
            }
        }

    }
}
