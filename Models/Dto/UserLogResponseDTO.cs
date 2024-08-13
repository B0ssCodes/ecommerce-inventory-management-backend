using System.Text.Json;

namespace Inventory_Management_Backend.Models.Dto
{
    public class UserLogResponseDTO
    {
        public int LogID { get; set; }
        public string LogName { get; set; }
        public string Action { get; set; }
        public string Model { get; set; }
        public int UserID { get; set; }
        public JsonElement BeforeState { get; set; }
        public JsonElement AfterState { get; set; }
    }
}
