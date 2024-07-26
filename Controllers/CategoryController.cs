using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Inventory_Management_Backend.Controllers
{
    [Route("api/category")]
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

        [HttpGet]
        [Route("get")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                List<CategoryResponseDTO> response = await _categoryRepository.GetCategories();
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Categories retrieved successfully";
                _response.Result = response;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                _response.Result = null;
                return BadRequest(_response);
            }
        }

        [HttpGet]
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
                _response.Result = null;
                return BadRequest(_response);
            }
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateCategory([FromBody] string categoryName)
        {
            try
            {
                CategoryResponseDTO category = await _categoryRepository.CreateCategory(categoryName);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Category created successfully";
                _response.Result = category;
                return Ok(_response);
            }
            catch (Exception ex)
            {
               _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                _response.Result = null;
                return BadRequest(_response);
            }
        }   

        [HttpDelete]
        [Route("delete/{categoryID}")]
        public async Task<IActionResult> DeleteCategory(int categoryID)
        {
            try
            {
                bool response = await _categoryRepository.DeleteCategory(categoryID);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Category deleted successfully";
                _response.Result = response;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                _response.Result = null;
                return BadRequest(_response);
            }
        }

        [HttpPut]
        [Route("update")]
        public async Task<IActionResult> UpdateCategory(CategoryRequestDTO requestDTO)
        {
            try
            {
                CategoryResponseDTO category = await _categoryRepository.UpdateCategory(requestDTO);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Category updated successfully";
                _response.Result = category;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                _response.Result = null;
                return BadRequest(_response);
            }
        }

    }
}
