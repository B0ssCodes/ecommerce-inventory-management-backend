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

        public ProductController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
            _response = new ApiResponse();
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
    }
}
