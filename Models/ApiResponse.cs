using System.Net;

namespace Inventory_Management_Backend.Models
{
    public class ApiResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string? Message { get; set; } 
        public object? Result { get; set; } 
        public int? ItemCount { get; set; }
    }
}