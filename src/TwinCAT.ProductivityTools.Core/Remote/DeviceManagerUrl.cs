namespace TwinCAT.ProductivityTools.Remote
{
	/// <summary>
	/// Builds the address of the Beckhoff Device Manager of a target.
	/// </summary>
	/// <remarks>
	/// The Device Manager is the web interface of a Beckhoff image. It always listens on HTTPS, so
	/// the scheme is not configurable; the certificate is self signed, which is why the browser
	/// warns before it shows the page.
	/// </remarks>
	public static class DeviceManagerUrl
	{
		public const string Scheme = "https://";
		public const string ConfigPath = "/config";

		/// <summary>
		/// Returns the Device Manager URL of a target, or <c>null</c> when the address is unusable.
		/// </summary>
		public static string For(string address)
		{
			string host = TargetAddress.ForUrl(address);

			return host == null ? null : Scheme + host + ConfigPath;
		}
	}
}
