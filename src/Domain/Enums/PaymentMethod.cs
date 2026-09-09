namespace Domain.Enums;

public enum PaymentMethod
{
    Cash = 1,
    Card = 2,
    /// <summary>Borca yazılıb — müştərinin hesabına əlavə olunur, dərhal ödəniş edilmir.</summary>
    Credit = 3
}
