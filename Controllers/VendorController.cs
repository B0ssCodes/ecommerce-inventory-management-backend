using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Inventory_Management_Backend.Controllers
{
    [Route("api/vendor")]
    [ApiController]
    public class VendorController : ControllerBase
    {
        private readonly IVendorRepository _vendorRepository;
        private readonly ApiResponse _response;

        public VendorController(IVendorRepository vendorRepository, ApiResponse response)
        {
            _vendorRepository = vendorRepository;
            _response = response;
        }

        [HttpPost]
        [Route("get")]
        public async Task<IActionResult> GetVendors(PaginationParams paginationParams)
        {
            try
            {
                List<VendorResponseDTO> vendorResponseDTOs = await _vendorRepository.GetVendors(paginationParams);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Vendors retrieved successfully";
                _response.Result = vendorResponseDTOs;
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
        [Route("get/{vendorID}")]
        public async Task<IActionResult> GetVendor(int vendorID)
        {
            try
            {
                VendorResponseDTO vendorResponseDTO = await _vendorRepository.GetVendor(vendorID);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Vendor retrieved successfully";
                _response.Result = vendorResponseDTO;
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
        [Route("create")]
        public async Task<IActionResult> CreateVendor(VendorRequestDTO vendorRequestDTO)
        {
            try
            {
                await _vendorRepository.CreateVendor(vendorRequestDTO);
                _response.StatusCode = HttpStatusCode.Created;
                _response.IsSuccess = true;
                _response.Message = "Vendor created successfully";
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
        [Route("update")]
        public async Task<IActionResult> UpdateVendor(int vendorID, VendorRequestDTO vendorRequestDTO)
        {
            try
            {
                await _vendorRepository.UpdateVendor(vendorID, vendorRequestDTO);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Vendor updated successfully";
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
        [Route("delete/{vendorID}")]
        public async Task<IActionResult> DeleteVendor(int vendorID)
        {
            try
            {
                await _vendorRepository.DeleteVendor(vendorID);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Vendor deleted successfully";
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
