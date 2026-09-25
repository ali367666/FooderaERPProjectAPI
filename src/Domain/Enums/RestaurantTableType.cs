namespace Domain.Enums;

public enum RestaurantTableType
{
    Masa = 1,
    Kabinet = 2,

    /// <summary>Auto-created, one-per-order virtual "table" that lets a delivery order satisfy
    /// the schema's required TableId without occupying a real dine-in table.</summary>
    Delivery = 3
}
