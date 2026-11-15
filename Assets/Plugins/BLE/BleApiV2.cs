using System;
using System.Runtime.InteropServices;

/// <summary>Versioned API; the legacy BleApi layout and exports remain unchanged.</summary>
public static class BleApiV2
{
    private const string DllName = "BleWinrtDll.dll";

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
    public struct DeviceUpdate
    {
        public ulong generation;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string name;
        public byte isConnectable;
        public byte advertisesHeartRate;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 8)]
    public struct OperationResult
    {
        public ulong generation;
        public int stage;
        public int status;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string message;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct HeartRateData
    {
        public ulong generation;
        public BleApi.BLEData data;
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint GetBleApiVersion();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint GetBleApiStructSizeV2(int kind);

    [DllImport(DllName, EntryPoint = "StartDeviceScanV2", CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong StartDeviceScan();

    [DllImport(DllName, EntryPoint = "StopDeviceScanV2", CallingConvention = CallingConvention.Cdecl)]
    public static extern void StopDeviceScan();

    [DllImport(DllName, EntryPoint = "PollDeviceV2", CallingConvention = CallingConvention.Cdecl)]
    public static extern BleApi.ScanStatus PollDevice(out DeviceUpdate device);

    [DllImport(DllName, EntryPoint = "ConnectHeartRateV2", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong ConnectHeartRate(string deviceId);

    [DllImport(DllName, EntryPoint = "CancelConnectionV2", CallingConvention = CallingConvention.Cdecl)]
    public static extern void CancelConnection();

    [DllImport(DllName, EntryPoint = "PollOperationV2", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool PollOperation(out OperationResult result);

    [DllImport(DllName, EntryPoint = "PollDataV2", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool PollData(out HeartRateData data);

    public static void ValidateAbi()
    {
        if (GetBleApiVersion() != 2)
            throw new InvalidOperationException("BLE library version mismatch. Install the matching x64 BleWinrtDll.dll.");
        CheckSize<DeviceUpdate>(1);
        CheckSize<OperationResult>(2);
        CheckSize<HeartRateData>(3);
    }

    private static void CheckSize<T>(int kind)
    {
        if (GetBleApiStructSizeV2(kind) != Marshal.SizeOf(typeof(T)))
            throw new InvalidOperationException("BLE library ABI mismatch: " + typeof(T).Name);
    }
}
