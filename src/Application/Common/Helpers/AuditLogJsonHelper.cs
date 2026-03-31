using System.Text.Json;

namespace Application.Common.Helpers;

public static class AuditLogJsonHelper
{
    public static string ToJson(object obj)
    {
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }
}