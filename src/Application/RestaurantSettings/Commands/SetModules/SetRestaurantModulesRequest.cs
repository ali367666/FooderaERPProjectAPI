namespace Application.RestaurantSettings.Commands.SetModules;

public class SetRestaurantModulesRequest
{
    public bool ModuleAnbar { get; set; }
    public bool ModuleRezervasyon { get; set; }
    public bool ModuleMasaBolge { get; set; }
    public bool ModulePaket { get; set; }
    public bool ModuleOtel { get; set; }
    public bool ModuleFitnes { get; set; }
    public bool ModuleDataSecimi { get; set; }
    public bool ModuleQiymetSor { get; set; }
}
