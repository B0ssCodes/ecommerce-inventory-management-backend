using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto.WarehouseDTO;
using Inventory_Management_Backend.Repository;
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
        private readonly IWarehouseFloorRepository _warehouseFloorRepository;
        private readonly IWarehouseRoomRepository _warehouseRoomRepository;
        private readonly IWarehouseAisleRepository _warehouseAisleRepository;
        private readonly IWarehouseShelfRepository _warehouseShelfRepository;
        private readonly IWarehouseBinRepository _warehouseBinRepository;

        public WarehouseController(IWarehouseRepository warehouseRepository,
                                   IWarehouseFloorRepository warehouseFloorRepository,
                                   IWarehouseRoomRepository warehouseRoomRepository,
                                   IWarehouseAisleRepository warehouseAisleRepository,
                                   IWarehouseShelfRepository warehouseShelfRepository,
                                   IWarehouseBinRepository warehouseBinRepository,
                                   ApiResponse response)
        {
            _warehouseRepository = warehouseRepository;
            _warehouseFloorRepository = warehouseFloorRepository;
            _warehouseRoomRepository = warehouseRoomRepository;
            _warehouseAisleRepository = warehouseAisleRepository;
            _warehouseShelfRepository = warehouseShelfRepository;
            _warehouseBinRepository = warehouseBinRepository;
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

        [HttpGet]
        [Route("get/floors/{warehouseID}")]
        public async Task<IActionResult> GetFloors(int warehouseID)
        {
            try
            {
                var result = await _warehouseFloorRepository.GetFloors(warehouseID);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Floors retrieved successfully";
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

        [HttpGet]
        [Route("get/rooms/{floorID}")]
        public async Task<IActionResult> GetRooms(int floorID)
        {
            try
            {
                var result = await _warehouseRoomRepository.GetRooms(floorID);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Rooms retrieved successfully";
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

        [HttpGet]
        [Route("get/aisles/{roomID}")]
        public async Task<IActionResult> GetAisles(int roomID)
        {
            try
            {
                var result = await _warehouseAisleRepository.GetAisles(roomID);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Aisles retrieved successfully";
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

        [HttpGet]
        [Route("get/shelves/{aisleID}")]
        public async Task<IActionResult> GetShelves(int aisleID)
        {
            try
            {
                var result = await _warehouseShelfRepository.GetShelves(aisleID);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Shelves retrieved successfully";
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

        [HttpGet]
        [Route("get/bins/{shelfID}")]
        public async Task<IActionResult> GetBins(int shelfID)
        {
            try
            {
                var result = await _warehouseBinRepository.GetBins(shelfID);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Bins retrieved successfully";
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
    }
}
