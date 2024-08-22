using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto.WarehouseDTO;
using Inventory_Management_Backend.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Inventory_Management_Backend.Controllers
{
    [Route("api/location")]
    [ApiController]
    public class InventoryLocationController : ControllerBase
    {
        private readonly ApiResponse _response;
        private readonly IInventoryLocationRepository _inventoryLocationRepository;

        public InventoryLocationController(IInventoryLocationRepository inventoryLocationRepository, ApiResponse response)
        {
            _inventoryLocationRepository = inventoryLocationRepository;
            _response = response;
        }

        [HttpGet]
        [Route("get/{locationID}")]
        public async Task<IActionResult> GetInventoryLocation(int locationID)
        {
            try
            {
                var result = await _inventoryLocationRepository.GetInventoryLocation(locationID);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Inventory location retrieved successfully";
                _response.Result = result;
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
        public async Task<IActionResult> CreateInventoryLocation(InventoryLocationRequestDTO requestDTO)
        {
            try
            {
                await _inventoryLocationRepository.CreateInventoryLocation(requestDTO);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Inventory location created successfully";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return BadRequest(_response);
            }

        }

        [HttpPut]
        [Route("update/{locationID}")]
        public async Task<IActionResult> UpdateInventoryLocation(int locationID, InventoryLocationUpdateDTO updateDTO)
        {
            try
            {
                await _inventoryLocationRepository.UpdateInventoryLocation(locationID, updateDTO);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Inventory location updated successfully";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return BadRequest(_response);
            }
        }

        [HttpDelete]
        [Route("delete/{locationID}")]
        public async Task<IActionResult> DeleteInventoryLocation(int locationID)
        {
            try
            {
                await _inventoryLocationRepository.DeleteInventoryLocation(locationID);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Inventory location deleted successfully";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                return BadRequest(_response);
            }
        }
    }
}
