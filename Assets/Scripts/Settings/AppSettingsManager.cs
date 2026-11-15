using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class AppSettings
{
    public string DeviceID = string.Empty;
    public string DeviceName = string.Empty;
    public List<string> VerifiedHeartRateDeviceIds = new List<string>();
    public float MinValue = 40f;
    public float MaxValue = 140f;
    public string QuarterHRSendTarget = string.Empty;
    public string NormalizedValueSendTarget = string.Empty;
    public string SendTargetIP = "127.0.0.1";
    public int SendTargetPort = 0;
}

public static class AppSettingsManager
{
    // Validation can isolate its settings without changing the user's persisted configuration.
    public static string SettingsPathOverride { get; set; }
    private static string FilePath =>
        string.IsNullOrEmpty(SettingsPathOverride) ? Path.Combine(Application.persistentDataPath, "settings.json") : SettingsPathOverride;

    public static void Save(AppSettings settings)
    {
        string json = JsonUtility.ToJson(settings, true);
        string directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
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
            if (settings == null) settings = new AppSettings();
            if (settings.VerifiedHeartRateDeviceIds == null) settings.VerifiedHeartRateDeviceIds = new List<string>();
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
    public static void SaveVerifiedDeviceInfo(string deviceID, string deviceName)
    {
        Load(out AppSettings settings);
        settings.DeviceID = deviceID;
        settings.DeviceName = deviceName;
        if (!string.IsNullOrEmpty(deviceID) && !settings.VerifiedHeartRateDeviceIds.Contains(deviceID))
            settings.VerifiedHeartRateDeviceIds.Add(deviceID);
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
