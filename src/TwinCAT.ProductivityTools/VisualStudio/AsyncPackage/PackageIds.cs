using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwinCAT.ProductivityTools
{
    internal class CommandIds
    {
        public const int ShutdownCommand = 0x0100;
        public const int RestartCommand = 0x0101;
        public const int DeviceInfoCommand = 0x0102;
        public const int RemoteDesktopCommand = 0x0103;
        public const int RteInstallCommand = 0x0104;
        public const int SetTickCommand = 0x0105;
        public const int AboutCommand = 0x0106;
    }

    [SuppressMessage("StyleCop", "SA1402", Justification = "This class does not have implementation. Used for constants.")]
    internal class PackageGuids
    {
        public const string PackageGuidString = "7e8a71c6-214a-40a2-a302-026e804258d4";

        public static Guid GuidCommandPackageCmdSet { get; } = Guid.Parse("21943ca9-120d-4b5b-a4ad-031dfd8791d9");
    }
}
