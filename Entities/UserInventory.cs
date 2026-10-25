namespace JavaMaster.Api.Entities;

public class UserInventory
{
    public int UserId { get; set; }
    public User? User { get; set; }

    public int ShopItemId { get; set; }
    public ShopItem? ShopItem { get; set; }

    public int Quantity { get; set; }
}