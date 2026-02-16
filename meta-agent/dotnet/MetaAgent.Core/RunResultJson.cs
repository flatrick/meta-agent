using System.IO;
using System.Text.Json;

namespace MetaAgent.Core
{
    public static class RunResultJson
    {
        public static string? ReadString(string path, string propertyName)
        {
            using var doc = TryParse(path);
            if (doc == null)
            {
                return null;
            }

            if (!doc.RootElement.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return prop.GetString();
        }

        public static JsonDocument? TryParse(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return JsonDocument.Parse(File.ReadAllText(path));
            }
            catch
            {
                return null;
            }
        }
    }
}
