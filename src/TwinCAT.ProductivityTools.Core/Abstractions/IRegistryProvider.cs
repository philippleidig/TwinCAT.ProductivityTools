using System;
using Microsoft.Win32;

namespace TwinCAT.ProductivityTools.Abstractions
{
	/// <summary>
	/// Read access to the Windows registry. Exists so that everything reading machine state can be
	/// exercised without touching the real registry.
	/// </summary>
	public interface IRegistryProvider
	{
		/// <summary>
		/// Returns the value <paramref name="valueName"/> below <paramref name="subKey"/>, or
		/// <c>null</c> when the key or the value does not exist.
		/// </summary>
		object GetValue(RegistryHive hive, RegistryView view, string subKey, string valueName);
	}
}
