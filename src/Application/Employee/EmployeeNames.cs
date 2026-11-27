namespace Application.Employees;

public static class EmployeeNames
{
    /// <summary>
    /// "Ad Soyad" typed into one field: the first word is the first name, everything after it the last
    /// name ("Əli Məmmədov Rəşid" gives "Əli" and "Məmmədov Rəşid"). One word leaves the last name empty.
    /// </summary>
    public static (string FirstName, string LastName) Split(string? fullName)
    {
        var parts = (fullName ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Length == 0
            ? (string.Empty, string.Empty)
            : (parts[0], string.Join(' ', parts.Skip(1)));
    }
}
