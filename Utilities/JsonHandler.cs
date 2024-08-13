using System.Data;
using System.Text.Json;
using Dapper;

namespace Inventory_Management_Backend.Utilities
{
    public class JsonHandler
    {
        static JsonHandler()
        {
            // Register the custom type handler for JsonElement
            SqlMapper.AddTypeHandler(new JsonElementTypeHandler());
        }

        public static void Initialize()
        {
            // This method ensures that the static constructor runs.
            // You can call this method from your application startup.
        }
    }

    // The custom type handler can be defined here or separately
    public class JsonElementTypeHandler : SqlMapper.TypeHandler<JsonElement>
    {
        public override void SetValue(IDbDataParameter parameter, JsonElement value)
        {
            parameter.Value = value.GetRawText();
            parameter.DbType = DbType.String;
        }

        public override JsonElement Parse(object value)
        {
            if (value is string jsonString)
            {
                return JsonSerializer.Deserialize<JsonElement>(jsonString);
            }
            throw new InvalidCastException($"Unable to cast object of type {value.GetType()} to {typeof(JsonElement)}.");
        }
    }
}
