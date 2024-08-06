using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Inventory_Management_Backend.Controllers
{
    [Route("api/category")]
    [Authorize]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ApiResponse _response;

        public CategoryController(ICategoryRepository categoryRepository, ApiResponse response)
        {
            _categoryRepository = categoryRepository;
            _response = response;
        }

        [HttpPost]
        [Authorize]
        [Route("get")]
        public async Task<IActionResult> GetCategories(PaginationParams paginationParams)
        {
            try
            {
                var (categories, totalCount) = await _categoryRepository.GetCategories(paginationParams);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Categories retrieved successfully";
                _response.Result = categories;
                _response.ItemCount = totalCount; // Include the total count in the response
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                _response.Result = default;
                _response.ItemCount = null; // Ensure ItemCount is null in case of an error
                return BadRequest(_response);
            }
        }

        [HttpGet]
        [Authorize]
        [Route("get/{categoryID}")]
        public async Task<IActionResult> GetCategory(int categoryID)
        {
            try
            {
                CategoryResponseDTO response = await _categoryRepository.GetCategory(categoryID);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Category retrieved successfully";
                _response.Result = response;
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

        [HttpPost]
        [Authorize]
        [Route("create")]
        public async Task<IActionResult> CreateCategory(CategoryRequestDTO categoryDTO)
        {
            try
            {
                await _categoryRepository.CreateCategory(categoryDTO);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Category created successfully";
                _response.Result = default;
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

        [HttpDelete]
        [Authorize]
        [Route("delete/{categoryID}")]
        public async Task<IActionResult> DeleteCategory(int categoryID)
        {
            try
            {
                await _categoryRepository.DeleteCategory(categoryID);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Category deleted successfully";
                _response.Result = default;
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

        [HttpPut]
        [Authorize]
        [Route("update/{categoryID}")]
        public async Task<IActionResult> UpdateCategory(int categoryID, CategoryRequestDTO requestDTO)
        {
            try
            {
                await _categoryRepository.UpdateCategory(categoryID, requestDTO);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Category updated successfully";
                _response.Result = default;
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

    }
}
