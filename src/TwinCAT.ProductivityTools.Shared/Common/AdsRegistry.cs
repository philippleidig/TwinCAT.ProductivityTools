using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinCAT.Ads;


namespace TwinCAT.ProductivityTools
{
    public enum RegistryValueType
    {
        NONE = 0,   /* No value TYPE */
        SZ,								/* Unicode nul terminated STRING */
        EXPAND_SZ,						/* Unicode nul terminated STRING (with environment variable references) */
        BINARY,							/* Free form binary */
        DWORD, 							/* 32-bit number and REG_DWORD_LITTLE_ENDIAN (same as REG_DWORD) */
        DWORD_BIG_ENDIAN,				/* 32-bit number */
        LINK,							/* Symbolic Link (unicode) */
        MULTI_SZ,						/* Multiple Unicode strings */
        RESOURCE_LIST, 					/* Resource list in the resource map */
        FULL_RESOURCE_DESCRIPTOR,		/* Resource list in the hardware description */
        RESOURCE_REQUIREMENTS_LIST,		/* */
        QWORD							/* 64-bit number and REG_QWORD_LITTLE_ENDIAN (same as REG_QWORD) */
    }

    public static class AdsRegistry
    {
        public static async Task<string> QueryValueAsync(AmsNetId target, string subKey, string valueName)
        {
            using (AdsClient client = new AdsClient())
            {
                client.Connect(new AmsAddress(target, AmsPort.SystemService));

                var readBuffer = new Memory<byte>(new byte[255]);

                var data = new List<byte>();

                data.AddRange(System.Text.Encoding.UTF8.GetBytes(subKey));
                data.Add(new byte()); // End delimiter
                data.AddRange(System.Text.Encoding.UTF8.GetBytes(valueName));
                data.Add(new byte());

                var writeBuffer = new ReadOnlyMemory<byte>(data.ToArray());

                var result = await client.ReadWriteAsync(200, 0, readBuffer, writeBuffer, CancellationToken.None);
                result.ThrowOnError();
                return System.Text.Encoding.UTF8.GetString(readBuffer.ToArray(), 0, result.ReadBytes);
            }
        }

        public static async Task SetValueAsync(AmsNetId target, string subKey, string valueName, RegistryValueType type, IEnumerable<byte> data)
        {
            using (AdsClient client = new AdsClient())
            {
                client.Connect(new AmsAddress(target, AmsPort.SystemService));

                var writeBuffer = new List<byte>();

                writeBuffer.AddRange(System.Text.Encoding.UTF8.GetBytes(subKey));
                writeBuffer.Add(new byte()); // End delimiter
                writeBuffer.AddRange(System.Text.Encoding.UTF8.GetBytes(valueName));
                writeBuffer.Add(new byte());
                writeBuffer.AddRange(data);

                var result = await client.WriteAsync(200, 0, new ReadOnlyMemory<byte>(writeBuffer.ToArray()), CancellationToken.None);
                result.ThrowOnError();
            }
        }

    }
}
