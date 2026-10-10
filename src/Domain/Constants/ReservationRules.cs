namespace Domain.Constants;

/// <summary>
/// The choices an admin can pick from for the two reservation settings (0 = switched off). The POS
/// and the settings page offer exactly these.
/// </summary>
public static class ReservationRules
{
    /// <summary>Minutes before a reservation during which its table takes no new order.</summary>
    public static readonly int[] BlockMinuteOptions = [0, 30, 45, 60, 90];

    /// <summary>Minutes after the reservation time after which a guest who has not come is cancelled.</summary>
    public static readonly int[] AutoCancelMinuteOptions = [0, 60, 90, 120];
}
