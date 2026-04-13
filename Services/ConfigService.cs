using System.Configuration;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace CargoTransport.Desktop.Services;

public interface IConfigService
{
    string? GetValue(string key);
    string GetValueOrDefault(string key, string defaultValue);
    void SetValue(string key, string? value);
    void RemoveValue(string key);
}

public class ConfigService : IConfigService
{
    private readonly Dictionary<string, string?> _persistedValues = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _settingsFilePath;

    public ConfigService()
    {
        _settingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CargoTransport.Desktop",
            "settings.json");

        LoadPersistedValues();
    }

    public string? GetValue(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        if (_persistedValues.TryGetValue(key, out string? persistedValue))
        {
            return persistedValue;
        }

        return ConfigurationManager.AppSettings[key];
    }

    public string GetValueOrDefault(string key, string defaultValue)
    {
        return GetValue(key) ?? defaultValue;
    }

    public void SetValue(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (value is null)
        {
            _persistedValues.Remove(key);
        }
        else
        {
            _persistedValues[key] = value;
        }

        SavePersistedValues();
    }

    public void RemoveValue(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (_persistedValues.Remove(key))
        {
            SavePersistedValues();
        }
    }

    private void LoadPersistedValues()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(_settingsFilePath);
            var values = JsonSerializer.Deserialize<Dictionary<string, string?>>(json);
            if (values is null)
            {
                return;
            }

            foreach ((string key, string? value) in values)
            {
                _persistedValues[key] = value;
            }
        }
        catch
        {
            _persistedValues.Clear();
        }
    }

    private void SavePersistedValues()
    {
        string? directory = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(_persistedValues, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_settingsFilePath, json);
    }
}
