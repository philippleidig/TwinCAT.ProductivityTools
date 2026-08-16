using System;

namespace TwinCAT.ProductivityTools.DataTypes
{
	public enum EventSeverity : byte
	{
		HINT = 0x01,
		WARN = 0x02,
		ERROR = 0x04,
		LOG = 0x10,
		MSGBOX = 0x20,
		RESOURCE = 0x40,
		STRING = 0x80,
	}

	public class EventMessage
	{
		public DateTime TimeRaised { get; set; }
		public string Message { get; set; } = string.Empty;
		public int AdsPort { get; set; }
		public string Sender { get; set; } = string.Empty;
		public EventSeverity Severity { get; set; }
	}
}
