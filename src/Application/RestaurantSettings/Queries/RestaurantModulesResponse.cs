using Application.Common.Responce;
using MediatR;

namespace Application.RestaurantSettings.Queries;

public class RestaurantModulesResponse
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
