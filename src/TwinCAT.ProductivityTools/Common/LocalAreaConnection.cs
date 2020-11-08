using System;
using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;

namespace TwinCAT.Remote
{
    public class LocalAreaConnection
    {

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid InstanceId { get; set; } = Guid.Empty;
        public PhysicalAddress MacAddress { get; set; }
        public IPAddress IpAddress { get; set; }
        public IPAddress SubnetMask { get; set; }
        public IPAddress Gateway { get; set; }
        public bool DHCP { get; set; }

    }
}
