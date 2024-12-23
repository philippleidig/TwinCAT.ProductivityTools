using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TwinCAT.ProductivityTools
{
    public static class RemoteDesktop
    {
        public static void Connect (IPAddress ipAddress)
        {
            Connect(ipAddress.ToString());
        }
        public static void Connect (string ipAddress)
        {
            Process rdcProcess = new Process();

            string executable = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\mstsc.exe");

            if (executable != null)
            {
                rdcProcess.StartInfo.FileName = executable;
                rdcProcess.StartInfo.Arguments = "/v " + ipAddress;
                rdcProcess.Start();
            }
        }
    }
}
