using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Inventory_Management_Backend.Controllers
{
    [Route("api/mv")]
    [ApiController]
    public class MaterializedViewController : ControllerBase
    {
        private readonly IMaterializedViewRepository _mvRepository;
        private readonly ApiResponse _response;

        public MaterializedViewController(IMaterializedViewRepository mvRepository, ApiResponse response)
        {
            _mvRepository = mvRepository;
            _response = response;
        }

        [HttpGet]
        [Route("refresh/product")]
        public async Task<IActionResult> RefreshProduct()
        {
            try
            {
                await _mvRepository.RefreshProductMV();
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Product analytics refreshed successfully";
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

        [HttpGet]
        [Route("refresh/vendor")]
        public async Task<IActionResult> RefreshVendor()
        {
            try
            {
                await _mvRepository.RefreshVendorMV();
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Vendor analytics refreshed successfully";
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

        [HttpGet]
        [Route("refresh/category")]
        public async Task<IActionResult> RefreshCategory()
        {
            try
            {
                await _mvRepository.RefreshCategoryMV();
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Category analytics refreshed successfully";
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
