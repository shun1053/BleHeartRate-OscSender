using System;
using System.IO;
using UnityEngine;

[Serializable]
public class AppSettings
{
    public string DeviceID = string.Empty;
    public string DeviceName = string.Empty;
    public float MinValue = 40f;
    public float MaxValue = 140f;
    public string QuarterHRSendTarget = string.Empty;
    public string NormalizedValueSendTarget = string.Empty;
    public string SendTargetIP = "127.0.0.1";
    public int SendTargetPort = 0;
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
            settings = new AppSettings();
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
            settings = new AppSettings();
            return false;
        }
        return true;
    }

    public static void SaveDeviceInfo(string deviceID, string deviceName)
    {
        // Load or create and overwrite device settings
        Load(out AppSettings settings);
        settings.DeviceID = deviceID;
        settings.DeviceName = deviceName;
        Save(settings);
    }
    public static void SaveMinValue(float minValue)
    {
        // Load or create and overwrite device settings
        Load(out AppSettings settings);
        settings.MinValue = minValue;
        Save(settings);
    }
    public static void SaveMaxValue(float maxValue)
    {
        // Load or create and overwrite device settings
        Load(out AppSettings settings);
        settings.MaxValue = maxValue;
        Save(settings);
    }
    public static void SaveQuarterHRSendTarget(string quarterHRTarget)
    {
        Load(out AppSettings settings);
        settings.QuarterHRSendTarget = quarterHRTarget;
        Save(settings);
    }
    public static void SaveNormalizedValueSendTarget(string normalizedValueTarget)
    {
        Load(out AppSettings settings);
        settings.NormalizedValueSendTarget = normalizedValueTarget;
        Save(settings);
    }
    public static void SaveSendTargetIP(string ip)
    {
        Load(out AppSettings settings);
        settings.SendTargetIP = ip;
        Save(settings);
    }
    public static void SaveSendTargetPort(int port)
    {
        Load(out AppSettings settings);
        settings.SendTargetPort = port;
        Save(settings);
    }
}
