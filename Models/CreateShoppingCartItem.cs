namespace ShoppingCartList.Models
{
    internal class CreateShoppingCartItem
    {
        public string ItemName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
    internal class UpdateShoppingCartItem
    {
        public bool Collected { get; set; } = false;
    }
}
