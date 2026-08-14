using System;
using System.IO;
using System.Text;
using TwinCAT.ProductivityTools.DataTypes;

namespace TwinCAT.ProductivityTools.Events
{
	/// <summary>
	/// Turns the payload of an ADS logger notification into an <see cref="EventMessage"/>.
	/// </summary>
	public interface IEventMessageParser
	{
		EventMessage Parse(byte[] eventData);
	}

	/// <summary>
	/// Reads the binary layout the TwinCAT logger port sends:
	/// <c>FILETIME (8) | severity (4) | sender ADS port (4) | sender name (16) | message length (4) | message</c>.
	/// </summary>
	public sealed class BinaryEventMessageParser : IEventMessageParser
	{
		internal const int SenderLength = 16;
		internal const int HeaderLength = 8 + 4 + 4 + SenderLength + 4;

		/// <summary>
		/// A logger message is a few hundred characters at most. The cap keeps a corrupt length
		/// field from allocating hundreds of megabytes inside the notification callback.
		/// </summary>
		internal const int MaximumMessageLength = 64 * 1024;

		public EventMessage Parse(byte[] eventData)
		{
			if (eventData == null || eventData.Length < HeaderLength)
			{
				return null;
			}

			using (var stream = new MemoryStream(eventData, writable: false))
			using (var reader = new BinaryReader(stream))
			{
				long timestamp = reader.ReadInt64();
				var severity = (EventSeverity)reader.ReadInt32();
				int senderPort = reader.ReadInt32();
				string sender = ReadFixedString(reader, SenderLength);
				int messageLength = reader.ReadInt32();

				return new EventMessage
				{
					TimeRaised = ToDateTime(timestamp),
					AdsPort = senderPort,
					Sender = sender,
					Message = ReadMessage(reader, messageLength, eventData.Length),
					Severity = severity,
				};
			}
		}

		/// <summary>
		/// The declared length is treated as an upper bound only. Truncated notifications do occur
		/// and must not throw, because the exception would surface on the ADS callback thread.
		/// </summary>
		private static string ReadMessage(BinaryReader reader, int declaredLength, int totalLength)
		{
			if (declaredLength <= 0)
			{
				return string.Empty;
			}

			int available = totalLength - HeaderLength;
			int length = Math.Min(Math.Min(declaredLength, available), MaximumMessageLength);

			return length <= 0 ? string.Empty : Decode(reader.ReadBytes(length));
		}

		private static string ReadFixedString(BinaryReader reader, int length) =>
			Decode(reader.ReadBytes(length));

		private static string Decode(byte[] data)
		{
			int end = Array.IndexOf(data, (byte)0);

			return Encoding.UTF8.GetString(data, 0, end < 0 ? data.Length : end);
		}

		/// <summary>
		/// The timestamp is a Windows FILETIME. Values outside its valid range are reported by
		/// devices with an unset clock and are mapped to <see cref="DateTime.MinValue"/>.
		/// </summary>
		private static DateTime ToDateTime(long fileTime)
		{
			try
			{
				return DateTime.FromFileTime(fileTime);
			}
			catch (ArgumentOutOfRangeException)
			{
				return DateTime.MinValue;
			}
		}
	}
}
