using System;

namespace TwinCAT.ProductivityTools
{
    public class DeviceInfo
    {
        public string TargetType { get; set; } = string.Empty;
        public string HardwareModel { get; set; } = string.Empty;
        public string HardwareSerialNo { get; set; } = string.Empty;
        public string HardwareVersion { get; set; } = string.Empty;
        public string HardwareDate { get; set; } = string.Empty;
        public string HardwareCPU { get; set; } = string.Empty;

        public string ImageDevice { get; set; } = string.Empty;
        public string ImageVersion { get; set; } = string.Empty;
        public string ImageLevel { get; set; } = string.Empty;
        public string ImageOsName { get; set; } = string.Empty;
        public string ImageOsVersion { get; set; } = string.Empty;

        public Version TwinCATVersion { get; set; }

    }
}
