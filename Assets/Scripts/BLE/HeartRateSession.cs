using System;
using System.Collections.Generic;
using System.Diagnostics;

public enum HeartRateConnectionState { Idle, Scanning, Connecting, AwaitingFirstNotification, Connected, ReconnectWaiting }
public enum BleScanPoll { Processing, Available, Finished }

public struct BleDeviceSnapshot
{
    public ulong Generation;
    public string Id, Name;
    public bool IsConnectable, AdvertisesHeartRate;
}

public struct BleOperation
{
    public ulong Generation;
    public int Stage, Status;
    public string Message;
}

public struct BleHeartRatePacket
{
    public ulong Generation;
    public byte[] Data;
    public int Size;
}

public interface IBleBackend
{
    ulong StartDeviceScan();
    void StopDeviceScan();
    BleScanPoll PollDevice(out BleDeviceSnapshot device);
    ulong ConnectHeartRate(string id);
    void CancelConnection();
    bool PollOperation(out BleOperation operation);
    bool PollHeartRate(out BleHeartRatePacket packet);
    string GetError();
}

public interface IBleClock { double Now { get; } }

/// <summary>Device discovery and connection state independent of Unity, rendering and wall-clock time.</summary>
public sealed class HeartRateSession
{
    public sealed class Device
    {
        public string Id, Name;
        public bool IsConnectable, AdvertisesHeartRate, Verified, Visible;
        internal bool Dirty;
    }

    readonly IBleBackend backend;
    readonly IBleClock clock;
    readonly Stopwatch workTimer = new Stopwatch();
    readonly Dictionary<string, Device> devices = new Dictionary<string, Device>();
    readonly List<Device> orderedDevices = new List<Device>();
    readonly Queue<Device> pendingDevices = new Queue<Device>();
    readonly HashSet<string> verifiedIds;
    ulong scanGeneration, connectionGeneration;
    double deadline, lastNotification, nextDevicePublish, nextErrorPoll;
    bool automaticRecovery, showAll;
    int completedStage;
    string lastNativeError;

    public HeartRateConnectionState State { get; private set; }
    public string SelectedId { get; private set; }
    public string SelectedName { get; private set; }
    public int ReconnectAttempts { get; private set; }
    public double ScanTimeout = 30, ConnectionTimeout = 30, FirstNotificationTimeout = 10, NotificationTimeout = 5;
    public int MaxReconnectAttempts;
    public int MaxItemsPerTick = 64;
    public double MaxWorkMilliseconds = 2;
    public bool ShowAll { get { return showAll; } }
    public IReadOnlyList<Device> Devices { get { return orderedDevices; } }
    public bool IsBusy { get { return State == HeartRateConnectionState.Connecting || State == HeartRateConnectionState.AwaitingFirstNotification || State == HeartRateConnectionState.ReconnectWaiting; } }

    public event Action DevicesCleared;
    public event Action<Device> DeviceChanged;
    public event Action<HeartRateConnectionState> StateChanged;
    public event Action<int> StageSucceeded;
    public event Action<int> HeartRateReceived;
    public event Action<string, string> DeviceVerified;
    public event Action<string> Error;
    public event Action TimedOut, ReceptionReset, ReconnectFailed, ReturnToDeviceList;

    public HeartRateSession(IBleBackend backend, IBleClock clock, IEnumerable<string> verifiedDeviceIds = null)
    {
        this.backend = backend;
        this.clock = clock;
        verifiedIds = verifiedDeviceIds == null ? new HashSet<string>() : new HashSet<string>(verifiedDeviceIds);
    }

    public void StartScan()
    {
        Cancel(false);
        SelectedId = SelectedName = null;
        devices.Clear();
        orderedDevices.Clear();
        pendingDevices.Clear();
        DevicesCleared?.Invoke();
        scanGeneration = backend.StartDeviceScan();
        deadline = clock.Now + ScanTimeout;
        nextDevicePublish = clock.Now;
        SetState(HeartRateConnectionState.Scanning);
    }

    public void StopScan()
    {
        if (State != HeartRateConnectionState.Scanning) return;
        backend.StopDeviceScan();
        scanGeneration = 0;
        nextDevicePublish = clock.Now;
        SetState(HeartRateConnectionState.Idle);
    }

    public void SetShowAll(bool value)
    {
        if (showAll == value) return;
        showAll = value;
        foreach (Device device in orderedDevices)
        {
            device.Visible = showAll || device.AdvertisesHeartRate || device.Verified;
            MarkDirty(device);
        }
        // A user-initiated filter change is immediate; regular advertising updates use Tick's budget.
        int items = 0;
        PublishDevices(ref items, false);
    }

    public void Select(string id, string name, bool reconnectSavedDevice = false)
    {
        if (string.IsNullOrEmpty(id) || IsBusy || State == HeartRateConnectionState.Connected) return;
        StopScan();
        SelectedId = id;
        SelectedName = string.IsNullOrEmpty(name) ? "Unnamed device" : name;
        automaticRecovery = reconnectSavedDevice;
        ReconnectAttempts = 0;
        BeginConnection();
    }

    public void Cancel(bool returnToList = true)
    {
        if (State == HeartRateConnectionState.Scanning) backend.StopDeviceScan();
        if (connectionGeneration != 0) backend.CancelConnection();
        scanGeneration = connectionGeneration = 0;
        automaticRecovery = false;
        ReconnectAttempts = completedStage = 0;
        SetState(HeartRateConnectionState.Idle);
        if (returnToList) ReturnToDeviceList?.Invoke();
    }

    public void Tick()
    {
        workTimer.Restart();
        int items = 0;

        // Subscription completion must be processed before its first notification. Once subscribed,
        // notifications always take priority over operation/discovery UI traffic.
        bool notificationsPolled = State == HeartRateConnectionState.AwaitingFirstNotification || State == HeartRateConnectionState.Connected;
        if (notificationsPolled)
            PollNotifications(ref items);
        while (WithinBudget(items) && connectionGeneration != 0 && backend.PollOperation(out BleOperation operation))
        {
            items++;
            if (operation.Generation == connectionGeneration) HandleOperation(operation);
        }
        if (!notificationsPolled && (State == HeartRateConnectionState.AwaitingFirstNotification || State == HeartRateConnectionState.Connected))
            PollNotifications(ref items);

        double now = clock.Now;
        bool publishDue = now >= nextDevicePublish;
        // Give already queued UI changes a chance before taking more advertising updates, so
        // an adapter producing continuous traffic cannot starve the visible device list.
        if (publishDue) PublishDevices(ref items);
        while (State == HeartRateConnectionState.Scanning && WithinBudget(items))
        {
            BleScanPoll result = backend.PollDevice(out BleDeviceSnapshot update);
            if (result == BleScanPoll.Processing) break;
            if (result == BleScanPoll.Finished) { StopScan(); break; }
            items++;
            if (update.Generation == scanGeneration && !string.IsNullOrEmpty(update.Id)) MergeDevice(update);
        }
        if (publishDue)
        {
            nextDevicePublish = now + 0.1;
            PublishDevices(ref items);
        }
        if (now >= nextErrorPoll)
        {
            nextErrorPoll = now + 0.5;
            string message = backend.GetError();
            if (!string.IsNullOrEmpty(message) && message != "Ok" && message != lastNativeError)
                Error?.Invoke(message);
            lastNativeError = message;
        }

        if (State == HeartRateConnectionState.Scanning && now >= deadline) StopScan();
        else if (State == HeartRateConnectionState.Connecting && now >= deadline)
            Fail("Connection timed out. Select a device to try again.", false, true);
        else if (State == HeartRateConnectionState.AwaitingFirstNotification && now >= deadline)
            Fail("No heart rate notification received. Check the sensor and try again.", false, true);
        else if (State == HeartRateConnectionState.Connected && now - lastNotification >= NotificationTimeout)
            Fail("Heart rate notifications stopped. Reconnecting...", false, true);
        else if (State == HeartRateConnectionState.ReconnectWaiting && now >= deadline)
        {
            ReconnectAttempts++;
            BeginConnection();
        }
    }

    bool WithinBudget(int count) { return count < MaxItemsPerTick && workTimer.Elapsed.TotalMilliseconds < MaxWorkMilliseconds; }

    void PollNotifications(ref int items)
    {
        while (WithinBudget(items) && backend.PollHeartRate(out BleHeartRatePacket packet))
        {
            items++;
            if (packet.Generation != connectionGeneration) continue;
            if (!HeartRateParser.TryParseHeartRate(packet.Data, packet.Size, out HeartRateParser.HeartRateMeasurement measurement, out string error))
            {
                Error?.Invoke("Invalid heart rate notification: " + error);
                continue;
            }
            lastNotification = clock.Now;
            automaticRecovery = true;
            ReconnectAttempts = 0;
            SetState(HeartRateConnectionState.Connected);
            // Every notification is an event, including repeated BPM values, to preserve OSC timing.
            HeartRateReceived?.Invoke(measurement.HeartRate);
        }
    }

    void MergeDevice(BleDeviceSnapshot update)
    {
        if (!devices.TryGetValue(update.Id, out Device device))
        {
            device = new Device { Id = update.Id, Name = "Unnamed device", Verified = verifiedIds.Contains(update.Id) };
            devices.Add(update.Id, device);
            orderedDevices.Add(device);
            MarkDirty(device);
        }
        if (!string.IsNullOrEmpty(update.Name) && device.Name != update.Name) { device.Name = update.Name; MarkDirty(device); }
        if (device.IsConnectable != update.IsConnectable) { device.IsConnectable = update.IsConnectable; MarkDirty(device); }
        if (update.AdvertisesHeartRate && !device.AdvertisesHeartRate) { device.AdvertisesHeartRate = true; MarkDirty(device); }
        device.Visible = showAll || device.AdvertisesHeartRate || device.Verified;
    }

    void MarkDirty(Device device)
    {
        if (device.Dirty) return;
        device.Dirty = true;
        pendingDevices.Enqueue(device);
    }

    void PublishDevices(ref int items, bool useBudget = true)
    {
        while (pendingDevices.Count > 0 && (!useBudget || WithinBudget(items)))
        {
            Device device = pendingDevices.Dequeue();
            device.Dirty = false;
            items++;
            DeviceChanged?.Invoke(device);
        }
    }

    void BeginConnection()
    {
        completedStage = 0;
        connectionGeneration = backend.ConnectHeartRate(SelectedId);
        deadline = clock.Now + ConnectionTimeout;
        SetState(HeartRateConnectionState.Connecting);
    }

    void HandleOperation(BleOperation operation)
    {
        if (operation.Status != 0)
        {
            Fail(string.IsNullOrEmpty(operation.Message) ? "Unable to connect to this heart rate device." : operation.Message, operation.Status == 1, false);
            return;
        }
        if (State != HeartRateConnectionState.Connecting || operation.Stage != completedStage + 1) return;
        completedStage = operation.Stage;
        if (operation.Stage == 3)
        {
            verifiedIds.Add(SelectedId);
            if (devices.TryGetValue(SelectedId, out Device device))
            {
                device.Verified = device.Visible = true;
                MarkDirty(device);
            }
            DeviceVerified?.Invoke(SelectedId, SelectedName);
            deadline = clock.Now + FirstNotificationTimeout;
            SetState(HeartRateConnectionState.AwaitingFirstNotification);
        }
        StageSucceeded?.Invoke(operation.Stage);
    }

    void Fail(string reason, bool unsupported, bool timeout)
    {
        bool resetReception = timeout || State == HeartRateConnectionState.Connected;
        backend.CancelConnection();
        connectionGeneration = 0;
        // Reset the displayed/transmitted value on every lost active reception, including native
        // disconnection/overflow errors. Timeout is a separate diagnostic, not another zero sample.
        if (resetReception) ReceptionReset?.Invoke();
        if (timeout) TimedOut?.Invoke();
        if (!unsupported && automaticRecovery && !string.IsNullOrEmpty(SelectedId))
        {
            if (MaxReconnectAttempts <= 0 || ReconnectAttempts < MaxReconnectAttempts)
            {
                deadline = clock.Now + Math.Min(30, Math.Pow(2, Math.Min(ReconnectAttempts, 5)));
                SetState(HeartRateConnectionState.ReconnectWaiting);
                Error?.Invoke(reason);
                return;
            }
            ReconnectFailed?.Invoke();
        }
        automaticRecovery = false;
        SetState(HeartRateConnectionState.Idle);
        ReturnToDeviceList?.Invoke();
        Error?.Invoke(reason);
    }

    void SetState(HeartRateConnectionState state)
    {
        if (State == state) return;
        State = state;
        StateChanged?.Invoke(state);
    }
}
