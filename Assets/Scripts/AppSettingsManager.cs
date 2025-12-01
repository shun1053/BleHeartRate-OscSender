using System;
using System.IO;
using UnityEngine;

[Serializable]
public class AppSettings
{
    public string DeviceID;
    public float MinValue;
    public float MaxValue;
}

public static class AppSettingsManager
{
    private static string FilePath =>
        Path.Combine(Application.persistentDataPath, "settings.json");

    public static void Save(AppSettings settings)
    {
        string json = JsonUtility.ToJson(settings, true);
        File.WriteAllText(FilePath, json);
    }

    public static bool Load(out AppSettings settings)
    {
        if (!File.Exists(FilePath))
        {
            settings = new AppSettings
            {
                DeviceID = "",
                MinValue = 0f,
                MaxValue = 1f
            };
            return false;
        }

        try
        {
            string json = File.ReadAllText(FilePath);
            settings = JsonUtility.FromJson<AppSettings>(json);
        }
        catch
        {
            // Create empty settings if JSON parsing failed
            settings = new AppSettings
            {
                DeviceID = "",
                MinValue = 0f,
                MaxValue = 1f
            };
            return false;
        }
        return true;
    }
}
