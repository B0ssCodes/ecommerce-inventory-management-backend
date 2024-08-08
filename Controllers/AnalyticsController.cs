using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Inventory_Management_Backend.Controllers
{
    [Route("api/analytics")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsRepository _analyticsRepository;
        private readonly ApiResponse _response;

        public AnalyticsController(IAnalyticsRepository analyticsRepository, ApiResponse response)
        {
            _analyticsRepository = analyticsRepository;
            _response = response;
        }

        [HttpGet]
        [Route("getProduct/{refreshDays}")]
        public async Task<IActionResult> GetProductAnalytics(int refreshDays)
        {
            try
            {
                var result = await _analyticsRepository.GetProductAnalytics(refreshDays);
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

        [HttpGet]
        [Route("resetProduct")]
        public async Task<IActionResult> ResetProductAnalyticsCache()
        {
            try
            {
               await _analyticsRepository.ResetProductAnalyticsCache();
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Product analytics cache reset successfully";
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

        [HttpGet]
        [Route("getVendor/{VendorCount}")]
        public async Task<IActionResult> GetVendorAnalytics(int VendorCount)
        {
            try
            {
                var result = await _analyticsRepository.GetVendorAnalytics(VendorCount);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Vendor analytics fetched successfully";
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

        [HttpGet]
        [Route("getCategory/{CategoryCount}")]
        public async Task<IActionResult> GetCategoryAnalytics(int CategoryCount)
        {
            try
            {
                var result = await _analyticsRepository.GetCategoryAnalytics(CategoryCount);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Category analytics fetched successfully";
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
