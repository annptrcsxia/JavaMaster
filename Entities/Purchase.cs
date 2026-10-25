namespace JavaMaster.Api.Entities;

public class Purchase
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ShopItemId { get; set; }
    public int PackSize { get; set; }      // 5 or 10
    public int Quantity { get; set; }
    public int TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}