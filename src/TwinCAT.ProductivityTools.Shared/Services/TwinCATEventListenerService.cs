using System;
using System.IO;
using TwinCAT.Ads;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.DataTypes;

namespace TwinCAT.ProductivityTools.Services
{
	public class MessageOccuredEventArgs
	{
		public MessageOccuredEventArgs(EventMessage eventMessage)
		{
			Event = eventMessage;
		}

		public EventMessage Event { get; }
	}

	public class TwinCATEventListenerService : ITwinCATEventListenerService
	{
		private readonly AdsClient adsClient = new AdsClient();
		private uint handle;

		public event MessageOccuredEventHandler MessageOccured;

		public TwinCATEventListenerService() { }

		public void Connect(AmsNetId target)
		{
			adsClient.Connect(target, (int)AmsPort.Logger);
			var settings = new NotificationSettings(AdsTransMode.CyclicInContext, 0, 0);
			handle = adsClient.AddDeviceNotification(0x1, 0xffff, 1024, settings, null);
			adsClient.AdsNotification += OnEventOccured;
		}

		private void OnEventOccured(object sender, AdsNotificationEventArgs e)
		{
			var eventMessage = ParseEvent(e.Data);
			MessageOccured?.Invoke(this, new MessageOccuredEventArgs(eventMessage));
		}

		public void Disconnect()
		{
			adsClient.AdsNotification -= OnEventOccured;
			adsClient.DeleteDeviceNotification(handle);
			adsClient.Disconnect();
		}

		private EventMessage ParseEvent(ReadOnlyMemory<byte> eventData)
		{
			using (MemoryStream ms = new MemoryStream(eventData.ToArray()))
			{
				using (BinaryReader reader = new BinaryReader(ms))
				{
					var timestamp = reader.ReadInt64();
					var timeRaised = DateTime.FromFileTime(timestamp);
					var logLevel = (EventSeverity)reader.ReadInt32();
					var senderPort = reader.ReadInt32();
					var senderData = reader.ReadBytes(16);
					var sender = System.Text.Encoding.UTF8.GetString(senderData).Trim('\0');
					var messageLength = reader.ReadInt32();
					var messageData = reader.ReadBytes(messageLength);
					var message = System.Text.Encoding.UTF8.GetString(messageData).Trim('\0');

					return new EventMessage
					{
						TimeRaised = timeRaised,
						AdsPort = senderPort,
						Sender = sender,
						Message = message,
						Severity = logLevel
					};
				}
			}
		}
	}
}
