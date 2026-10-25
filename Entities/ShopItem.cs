namespace JavaMaster.Api.Entities;

public class ShopItem
{
    public int Id { get; set; }
    public string Code { get; set; } = "";   // HINT, HEAL_POTION, STREAK_REPAIR
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Price5Pack { get; set; }
    public int Price10Pack { get; set; }
    public bool IsActive { get; set; } = true;
}