﻿using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using Inventory_Management_Backend.Utilities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Inventory_Management_Backend.Controllers
{
    [Route("api/transaction")]
    //[Authorize]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly ApiResponse _response;

        public TransactionController(ITransactionRepository transactionRepository, ApiResponse response)
        {
            _transactionRepository = transactionRepository;
            _response = response;
        }

        [HttpPost]
        [Route("get")]
        public async Task<IActionResult> GetTransactions(VendorTransactionDTO transactionDTO)
        {
            try
            {

                var (transactions, itemCount) = await _transactionRepository.GetTransactions(transactionDTO.VendorEmail, transactionDTO.PaginationParams);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Transactions fetched successfully";
                _response.Result = transactions;
                _response.ItemCount = itemCount;
                return Ok(_response);
            }
            catch (Exception ex) {

                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.Message = ex.Message;
                _response.Result = default;
                return BadRequest(_response);
            }

        }

      
        [HttpGet]
        [Route("get/{transactionID}/{transactionTypeID}")]
        public async Task<IActionResult> GetTransaction(int transactionID, int transactionTypeID)
        {
            try
            {
                if (transactionTypeID == (int)TransactionTypeEnum.Inbound)
                {
                    var transaction = await _transactionRepository.GetInboundTransaction(transactionID);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Message = "Transaction fetched successfully";
                    _response.Result = transaction;
                }
                else if (transactionTypeID == (int)TransactionTypeEnum.Outbound)
                {
                    var transaction = await _transactionRepository.GetOutboundTransaction(transactionID);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    _response.Message = "Transaction fetched successfully";
                    _response.Result = transaction;
                }
                
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
        [Route("getCount")]
        public async Task<IActionResult> GetOutboundTransactionCount()
        {
            try
            {
                int transactionCount = await _transactionRepository.GetOutboundTransactionsCount();
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Transaction count fetched successfully";
                _response.Result = transactionCount;
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
        public async Task<IActionResult> CreateTransaction(TransactionCreateDTO createDTO)
        {
            try
            {
                int transactionID = await _transactionRepository.CreateTransaction(createDTO);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Transaction created successfully";
                _response.Result = transactionID;
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
        [Route("submit")]
        public async Task<IActionResult> SubmitTransaction(TransactionSubmitDTO submitDTO)
        {
            try
            {
                await _transactionRepository.SubmitTransaction(submitDTO);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Transaction submitted successfully";
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
        [Route("delete/{transactionID}")]
        public async Task<IActionResult> DeleteTransaction(int transactionID)
        {
            try
            {
                await _transactionRepository.DeleteTransaction(transactionID);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Transaction deleted successfully";
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
