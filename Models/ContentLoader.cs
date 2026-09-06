using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WpfApp1.Models;

public static class ContentLoader
{
    private static List<Theme>? _cache;

    public static List<Theme> Load()
    {
        if (_cache != null) return _cache;

        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "themes.json");
        if (!File.Exists(path))
            return _cache = ThemeData.GetAll();

        try
        {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var data = JsonSerializer.Deserialize<ThemeDataJson>(json, options);
            var result = data?.Chapters;
            if (result == null || result.Count == 0)
                return _cache = ThemeData.GetAll();
            return _cache = result;
        }
        catch
        {
            return _cache = ThemeData.GetAll();
        }
    }

    public static void ClearCache() => _cache = null;
}

public class ThemeDataJson
{
    public List<Theme> Chapters { get; set; } = new();
}