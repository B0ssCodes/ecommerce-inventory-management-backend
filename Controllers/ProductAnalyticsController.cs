using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Inventory_Management_Backend.Controllers
{
    [Route("api/analytics")]
    [ApiController]
    public class ProductAnalyticsController : ControllerBase
    {
        private readonly IProductAnalyticsRepository _productAnalyticsRepository;
        private readonly ApiResponse _response;

        public ProductAnalyticsController(IProductAnalyticsRepository productAnalyticsRepository, ApiResponse response)
        {
            _productAnalyticsRepository = productAnalyticsRepository;
            _response = response;
        }

        [HttpGet]
        [Route("get")]
        public async Task<IActionResult> GetProductAnalytics()
        {
            try
            {
                var result = await _productAnalyticsRepository.GetProductAnalytics();
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Product analytics fetched successfully";
                _response.Result = result;
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
