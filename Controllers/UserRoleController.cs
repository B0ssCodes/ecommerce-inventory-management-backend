using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
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
        private readonly ApiResponse _response;

        public UserRoleController(IUserRoleRepository userRoleRepository, ApiResponse apiResponse)
        {
            _userRoleRepository = userRoleRepository;
            _response = apiResponse;
        }

        [HttpPost]
        [Route("get")]
        public async Task<IActionResult> GetUserRoles(PaginationParams paginationParams)
        {
            try
            {
                var (userRoles, totalCount) = await _userRoleRepository.GetUserRoles(paginationParams);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "User roles retrieved successfully";
                _response.Result = userRoles;
                _response.ItemCount = totalCount;
                return Ok(_response);
            }
            
            catch (Exception Ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.Message = Ex.Message;
                _response.Result = null;
                return BadRequest(_response);
            }
        }

        [HttpGet]
        [Route("get/{userRoleId}")]
        public async Task<IActionResult> GetUserRole(int userRoleId)
        {
            try
            {
                UserRoleDTO userRole = await _userRoleRepository.GetUserRole(userRoleId);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "User role retrieved successfully";
                _response.Result = userRole;
                return Ok(_response);
            }
            
            catch (Exception Ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.Message = Ex.Message;
                _response.Result = null;
                return BadRequest(_response);
            }
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateUserRole(UserRoleRequestDTO requestDTO)
        {
            try
            {
                 await _userRoleRepository.CreateUserRole(requestDTO);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "User role created successfully";
                _response.Result = default;
                return Ok(_response);
            }
            
            catch (Exception Ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.Message = Ex.Message;
                _response.Result = null;
                return BadRequest(_response);
            }
        }

        [HttpPut]
        [Route("update/{roleId}")]
        public async Task<IActionResult> UpdateUserRole(int roleId, UserRoleRequestDTO requestDTO)
        {
            try
            {
                await _userRoleRepository.UpdateUserRole(roleId, requestDTO);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "User role updated successfully";
                _response.Result = default;
                return Ok(_response);
            }
            
            catch (Exception Ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.Message = Ex.Message;
                _response.Result = null;
                return BadRequest(_response);
            }
        }

        [HttpDelete]
        [Route("delete/{roleId}")]
        public async Task<IActionResult> DeleteUserRole(int roleId)
        {
            try
            {
                await _userRoleRepository.DeleteUserRole(roleId);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "User role deleted successfully";
                _response.Result = null;
                return Ok(_response);
            }
            
            catch (Exception Ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.Message = Ex.Message;
                _response.Result = null;
                return BadRequest(_response);
            }
        }

    }
}
