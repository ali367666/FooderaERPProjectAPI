namespace Application.RestaurantTable.Commands.UpdateLayout;

public class UpdateTableLayoutRequest
{
    public int PosX { get; set; }
    public int PosY { get; set; }
    public int Width { get; set; } = 80;
    public int Height { get; set; } = 80;
    public string Shape { get; set; } = "square";
    public int Rotation { get; set; }
}
