using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Inventory_Management_Backend.Controllers
{
    [Route("api/inventory")]

    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryRepository _inventoryRepository;
        private readonly ApiResponse _response;

        public InventoryController(IInventoryRepository inventoryRepository, ApiResponse response)
        {
            _inventoryRepository = inventoryRepository;
            _response = response;
        }

        [HttpPost]
        [Route("get")]
        public async Task<IActionResult> GetInventories(PaginationParams paginationParams)
        {
            try
            {
                var (inventories, totalCount) = await _inventoryRepository.GetInventories(paginationParams);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Inventories fetched successfully";
                _response.Result = inventories;
                _response.ItemCount = totalCount;
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
        [Route("get/{inventoryID}")]
        public async Task<IActionResult> GetInventory(int inventoryID)
        {
            try
            {
                var inventory = await _inventoryRepository.GetInventory(inventoryID);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Inventory fetched successfully";
                _response.Result = inventory;
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
        [Route("getlow")]
        public async Task<IActionResult> GetLowStockInventories(PaginationParams paginationParams)
        {
            try
            {
                var (inventories, totalCount) = await _inventoryRepository.GetLowStockInventories(paginationParams);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Low stock inventories fetched successfully";
                _response.Result = inventories;
                _response.ItemCount = totalCount;
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
        [Route("getout")]
        public async Task<IActionResult> GetOutStockInventories(PaginationParams paginationParams)
        {
            try
            {
                var (inventories, totalCount) = await _inventoryRepository.GetOutStockInventories(paginationParams);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Out of stock inventories fetched successfully";
                _response.Result = inventories;
                _response.ItemCount = totalCount;
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
