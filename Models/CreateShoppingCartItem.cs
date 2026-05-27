namespace ShoppingCartList.Models
{
    public class CreateShoppingCartItem
    {
        public string ItemName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
    public class UpdateShoppingCartItem
    {
        public bool Collected { get; set; } = false;
    }
}
