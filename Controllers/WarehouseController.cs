using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto.WarehouseDTO;
using Inventory_Management_Backend.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Inventory_Management_Backend.Controllers
{
    [Route("api/warehouse")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly ApiResponse _response;
        private readonly IWarehouseRepository _warehouseRepository;

        public WarehouseController(IWarehouseRepository warehouseRepository, ApiResponse response)
        {
            _warehouseRepository = warehouseRepository;
            _response = response;
        }

        [HttpPost]
        [Route("get")]
        public async Task<IActionResult> GetWarehouses(PaginationParams paginationParams)
        {
            try
            {
                var (result, totalCount) = await _warehouseRepository.GetWarehouses(paginationParams);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Warehouses retrieved successfully";
                _response.Result = result;
                _response.ItemCount = totalCount;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                _response.Result = default;
                _response.ItemCount = default;
                return BadRequest(_response);
            }
        }

        [HttpGet]
        [Route("get/{warehouseID}")]
        public async Task<IActionResult> GetWarehouse(int warehouseID)
        {
            try
            {
                var result = await _warehouseRepository.GetWarehouse(warehouseID);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Warehouse retrieved successfully";
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
        public async Task<IActionResult> CreateWarehouse(WarehouseRequestDTO requestDTO)
        {
            try
            {
                await _warehouseRepository.CreateWarehouse(requestDTO);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Warehouse created successfully";
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
        [Route("update/{warehouseID}")]
        public async Task<IActionResult> UpdateWarehouse(int warehouseID, WarehouseRequestDTO requestDTO)
        {
            try
            {
                await _warehouseRepository.UpdateWarehouse(warehouseID, requestDTO);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Warehouse updated successfully";
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
        [Route("delete/{warehouseID}")]
        public async Task<IActionResult> DeleteWarehouse(int warehouseID)
        {
            try
            {
                await _warehouseRepository.DeleteWarehouse(warehouseID);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Warehouse deleted successfully";
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
