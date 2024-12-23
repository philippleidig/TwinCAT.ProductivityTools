using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TwinCAT.ProductivityTools.Abstractions
{
	internal interface IOutputWindowPane
	{
		Task WriteLineAsync();
		Task WriteLineAsync(string value);
	}
}
