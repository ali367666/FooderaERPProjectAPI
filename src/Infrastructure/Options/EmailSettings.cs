namespace Infrastructure.Options;

public class EmailSettings
{
    public string Host { get; set; } = default!;
    public int Port { get; set; }
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string FromName { get; set; } = default!;
    public string FromEmail { get; set; } = default!;
    public bool UseSsl { get; set; }
}