using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace ShoppingCartList.Models
{
    public class ShoppingCartItem : TableEntity
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ItemName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool Collected { get; set; } = false;

        [JsonProperty("category")]
        public string Category { get; set; } = string.Empty;
    }
}
