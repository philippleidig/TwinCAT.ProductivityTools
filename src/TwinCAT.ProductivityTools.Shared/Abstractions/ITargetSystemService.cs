using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TwinCAT.Ads;
using TwinCAT.ProductivityTools.Services;

namespace TwinCAT.ProductivityTools.Abstractions
{
	//[Guid("7ac9e7cb-f32a-44e7-86d7-6341bc666dc0")]
	public interface ITargetSystemService
	{
		AmsNetId ActiveTargetSystem { get; }
		Task ShutdownAsync();
	}
}
