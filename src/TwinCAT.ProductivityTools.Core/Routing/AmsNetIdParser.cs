using TwinCAT.Ads;

namespace TwinCAT.ProductivityTools.Routing
{
	/// <summary>
	/// Parses an AmsNetId without letting malformed input escape as an exception.
	/// </summary>
	/// <remarks>
	/// <c>AmsNetId.TryParse</c> does not keep the promise its name makes: it throws an
	/// <see cref="System.ArgumentException"/> for null and for an empty string, and it throws
	/// whatever the underlying parser throws for input that is not remotely an address. Callers
	/// here read their target from a project property, from a route file or from a text box, so
	/// empty and malformed are ordinary cases rather than programming errors.
	/// </remarks>
	public static class AmsNetIdParser
	{
		/// <summary>
		/// Returns <c>true</c> and the parsed address, or <c>false</c> for anything that is not a
		/// usable AmsNetId.
		/// </summary>
		public static bool TryParse(string value, out AmsNetId netId)
		{
			netId = null;

			if (string.IsNullOrWhiteSpace(value))
			{
				return false;
			}

			try
			{
				string trimmed = value.Trim();

				if (!HasSixOctets(trimmed))
				{
					return false;
				}

				AmsNetId parsed;

				if (!AmsNetId.TryParse(trimmed, out parsed))
				{
					return false;
				}

				netId = parsed;

				return netId != null;
			}
			catch (System.Exception)
			{
				return false;
			}
		}

		/// <summary>
		/// Checks the shape before the ADS parser sees it.
		/// </summary>
		/// <remarks>
		/// The ADS parser truncates an out of range octet to a byte, so <c>300.1.1.1.1.1</c> is
		/// silently accepted as <c>44.1.1.1.1.1</c>. A typo in a target address that quietly
		/// becomes a different, reachable machine is worse than a rejected one, so the octets are
		/// range checked here.
		/// </remarks>
		private static bool HasSixOctets(string value)
		{
			string[] parts = value.Split('.');

			if (parts.Length != 6)
			{
				return false;
			}

			foreach (string part in parts)
			{
				byte octet;

				if (
					part.Length == 0
					|| part.Length > 3
					|| !byte.TryParse(
						part,
						System.Globalization.NumberStyles.None,
						System.Globalization.CultureInfo.InvariantCulture,
						out octet
					)
				)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Returns whether the value describes a usable AmsNetId.
		/// </summary>
		public static bool IsValid(string value)
		{
			AmsNetId netId;

			return TryParse(value, out netId);
		}
	}
}
