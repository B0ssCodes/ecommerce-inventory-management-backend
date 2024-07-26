using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using System.Net;
namespace Inventory_Management_Backend.Controllers
{
    [Route("api/product")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly ApiResponse _response;

        public ProductController(IProductRepository productRepository, ApiResponse response)
        {
            _productRepository = productRepository;
            _response = response;
        }

        [HttpGet]
        [Route("get")]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                List<ProductResponseDTO> products = await _productRepository.GetProducts();
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Products retrieved successfully";
                _response.Result = products;
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
        [Route("get/{productID}")]
        public async Task<IActionResult> GetProduct(int productID)
        {
            try
            {
                ProductResponseDTO product = await _productRepository.GetProduct(productID);
                if (product == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.Message = "Product not found";
                    _response.Result = null;
                    return NotFound(_response);
                }
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Product retrieved successfully";
                _response.Result = product;
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
        public async Task<IActionResult> CreateProduct([FromBody] ProductRequestDTO productDTO)
        {
            try
            {
                ProductResponseDTO product = await _productRepository.CreateProduct(productDTO);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Product created successfully";
                _response.Result = product;
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
        [Route("delete/{productID}")]
        public async Task<IActionResult> DeleteProduct(int productID)
        {
            try
            {
                bool isDeleted = await _productRepository.DeleteProduct(productID);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Product deleted successfully";
                _response.Result = null;
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
        [Route("update/{productID}")]
        public async Task<IActionResult> UpdateProduct(int productID, [FromBody] ProductRequestDTO productDTO)
        {
            try
            {
                ProductResponseDTO product = await _productRepository.UpdateProduct(productID,productDTO);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Product updated successfully";
                _response.Result = product;
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
