using System;
using System.Collections.Generic;

public static class HeartRateParser
{
    /// <summary>
    /// Bluetooth SIG Heart Rate Measurement payload structure
    /// UUID:0x2A37
    /// </summary>
    public struct HeartRateMeasurement
    {
        public byte Flags;
        public int PayloadLength;

        // Heart Rate Value
        public bool IsHeartRateUInt16;     // true:16bit/false:8bit
        public int HeartRate;              // bpm

        public bool SensorContactSupported;
        public bool SensorContactDetected;

        // Optional fields
        public ushort? EnergyExpended;     // units: kiloJoules
        public ushort[] RRIntervalsRaw;    // 1/1024s units
        public double[] RRIntervalsSec;    // seconds (raw / 1024.0)

        public override string ToString()
        {
            string rr = RRIntervalsRaw != null ? string.Join(", ", RRIntervalsRaw) : "-";
            return $"HR={HeartRate} bpm ({"UInt" + (IsHeartRateUInt16 ? "16" : "8")}), " +
                   $"Flags=0x{Flags:X2}, Energy={(EnergyExpended.HasValue ? EnergyExpended.Value.ToString() : "N/A")}, " +
                   $"RR(raw)=[{rr}]";
        }
    }
    
    public static bool TryParseHeartRate(byte[] data, int dataSize, out HeartRateMeasurement result, out string error)
    {
        result = default;
        error = null;

        if (data == null)
        {
            error = "data is null";
            return false;
        }

        if (dataSize < 2 || dataSize > data.Length)
        {
            error = "dataSize is invalid";
            return false;
        }

        int readIndex = 0;
        byte flags = data[readIndex++];
        bool hrIsUInt16 = (flags & 0x01) != 0;          // bit0
        bool contactDetected = (flags & 0x02) != 0;     // bit1
        bool contactSupported = (flags & 0x04) != 0;    // bit2
        bool hasEnergy = (flags & 0x08) != 0;           // bit3
        bool hasRR = (flags & 0x10) != 0;               // bit4

        // Heart Rate Measurement Value
        int heartRate;
        if (hrIsUInt16)
        {
            // 2-byte
            if (readIndex + 1 >= dataSize) { error = "missing UInt16 heart rate"; return false; }
            heartRate = data[readIndex++] | (data[readIndex++] << 8); // Little-Endian
        }
        else
        {
            // 1-byte
            if (readIndex >= dataSize) { error = "missing UInt8 heart rate"; return false; }
            heartRate = data[readIndex++];
        }

        // Energy Expended (optional, UInt16)
        ushort? energy = null;
        if (hasEnergy)
        {
            if (readIndex + 1 >= dataSize) { error = "missing Energy Expended field"; return false; }
            energy = (ushort)(data[readIndex++] | (data[readIndex++] << 8));
        }

        // RR-Intervals (optional, UInt16 x N)
        List<ushort> rrRaw = null;
        List<double> rrSec = null;
        if (hasRR)
        {
            rrRaw = new List<ushort>(Math.Max(0, (dataSize - readIndex) / 2));
            rrSec = new List<double>(rrRaw.Capacity);

            // Read each 2 bytes as one RR-Interval
            int remain = dataSize - readIndex;
            if ((remain & 0x01) != 0)
            {
                // Should be even number of bytes
                error = $"RR-Interval length is odd ({remain} bytes)";
                return false;
            }

            while (readIndex + 1 < dataSize)
            {
                ushort rr = (ushort)(data[readIndex++] | (data[readIndex++] << 8));
                rrRaw.Add(rr);
                rrSec.Add(rr / 1024d);
            }
        }

        result = new HeartRateMeasurement
        {
            Flags = flags,
            PayloadLength = dataSize,

            IsHeartRateUInt16 = hrIsUInt16,
            HeartRate = heartRate,

            SensorContactSupported = contactSupported,
            SensorContactDetected = contactDetected,

            EnergyExpended = energy,
            RRIntervalsRaw = rrRaw?.ToArray(),
            RRIntervalsSec = rrSec?.ToArray(),
        };

        return true;
    }
}
