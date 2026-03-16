namespace Domain.Enums;

public enum TransferStatus
{
    Created = 1,
    PickedUp = 2,       // A anbardan çıxış + Vehicle-ə giriş oldu
    InTransit = 3,
    Delivered = 4,      // B anbarda qəbul edildi
    Cancelled = 5
}
