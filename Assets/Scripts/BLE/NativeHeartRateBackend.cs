public sealed class NativeHeartRateBackend : IBleBackend
{
    public NativeHeartRateBackend() { BleApiV2.ValidateAbi(); }
    public ulong StartDeviceScan() { return BleApiV2.StartDeviceScan(); }
    public void StopDeviceScan() { BleApiV2.StopDeviceScan(); }
    public ulong ConnectHeartRate(string id) { return BleApiV2.ConnectHeartRate(id); }
    public void CancelConnection() { BleApiV2.CancelConnection(); }

    public BleScanPoll PollDevice(out BleDeviceSnapshot device)
    {
        BleApi.ScanStatus status = BleApiV2.PollDevice(out BleApiV2.DeviceUpdate value);
        device = new BleDeviceSnapshot
        {
            Generation = value.generation, Id = value.id, Name = value.name,
            IsConnectable = value.isConnectable != 0, AdvertisesHeartRate = value.advertisesHeartRate != 0
        };
        return (BleScanPoll)(int)status;
    }

    public bool PollOperation(out BleOperation operation)
    {
        bool available = BleApiV2.PollOperation(out BleApiV2.OperationResult value);
        operation = new BleOperation { Generation = value.generation, Stage = value.stage, Status = value.status, Message = value.message };
        return available;
    }

    public bool PollHeartRate(out BleHeartRatePacket packet)
    {
        bool available = BleApiV2.PollData(out BleApiV2.HeartRateData value);
        packet = new BleHeartRatePacket { Generation = value.generation, Data = value.data.buf, Size = value.data.size };
        return available;
    }

    public string GetError()
    {
        BleApi.GetError(out BleApi.ErrorMessage error);
        return error.msg;
    }
}
