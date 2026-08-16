using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TwinCAT.ProductivityTools.Installation;

namespace TwinCAT.ProductivityTools
{
	/// <summary>
	/// Reads the statically configured ADS routes of the local system.
	/// </summary>
	public interface IRouteReader
	{
		IEnumerable<TcConfigRoute> ListRoutes();
	}

	/// <summary>
	/// Reads <c>StaticRoutes.xml</c> from the local TwinCAT installation.
	/// </summary>
	public sealed class StaticRoutesReader : IRouteReader
	{
		private readonly ITwinCATInstallation installation;

		public StaticRoutesReader()
			: this(new TwinCATInstallation()) { }

		public StaticRoutesReader(ITwinCATInstallation installation)
		{
			this.installation =
				installation ?? throw new ArgumentNullException(nameof(installation));
		}

		public IEnumerable<TcConfigRoute> ListRoutes()
		{
			string path = installation.StaticRoutesPath;

			if (string.IsNullOrEmpty(path) || !File.Exists(path))
			{
				return Enumerable.Empty<TcConfigRoute>();
			}

			using (FileStream stream = File.OpenRead(path))
			{
				return Read(stream);
			}
		}

		/// <summary>
		/// Deserializes a <c>StaticRoutes.xml</c> document. A malformed document yields an empty
		/// sequence rather than an exception, because the routes only enrich the UI and a broken
		/// routing file must not take a command down.
		/// </summary>
		public static IEnumerable<TcConfigRoute> Read(Stream stream)
		{
			if (stream == null)
			{
				return Enumerable.Empty<TcConfigRoute>();
			}

			try
			{
				var serializer = new XmlSerializer(typeof(TcConfig));
				var config = (TcConfig)serializer.Deserialize(stream);

				return config?.RemoteConnections ?? Enumerable.Empty<TcConfigRoute>();
			}
			catch (InvalidOperationException)
			{
				return Enumerable.Empty<TcConfigRoute>();
			}
		}
	}

	/// <summary>
	/// Convenience entry point for call sites that do not need to inject a different installation.
	/// </summary>
	public static class AmsRouter
	{
		public static IEnumerable<TcConfigRoute> ListRoutes() =>
			new StaticRoutesReader().ListRoutes();
	}
}
