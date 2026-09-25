namespace Domain.Entities;

/// <summary>
/// Filial-səviyyəli modul dəsti — yalnız ana Company "İstirahət Kompleksi" (ModuleKompleks) rejimindədirsə
/// nəzərə alınır; o zaman bu Filialın öz modulları Company-nin ümumi modullarının üzərinə yazılır.
/// ModuleFilial burada yoxdur — "filial olmaq" statusu Company səviyyəsində qalır, filialın özündə mənasızdır.
/// </summary>
public class RestaurantSettings
{
    public int Id { get; set; }

    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = default!;

    public bool ModuleAnbar { get; set; }
    public bool ModuleRezervasyon { get; set; }
    public bool ModuleMasaBolge { get; set; }
    public bool ModulePaket { get; set; }
    public bool ModuleOtel { get; set; }
    public bool ModuleFitnes { get; set; }
    public bool ModuleDataSecimi { get; set; }
    public bool ModuleQiymetSor { get; set; }
}
