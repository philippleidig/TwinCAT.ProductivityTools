using System;
using System.IO;
using TwinCAT.Ads;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.DataTypes;
using TwinCAT.ProductivityTools.Events;

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
		private readonly IEventMessageParser parser = new BinaryEventMessageParser();
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
			EventMessage eventMessage = parser.Parse(e.Data.ToArray());

			if (eventMessage == null)
			{
				return;
			}

			MessageOccured?.Invoke(this, new MessageOccuredEventArgs(eventMessage));
		}

		public void Disconnect()
		{
			adsClient.AdsNotification -= OnEventOccured;
			adsClient.DeleteDeviceNotification(handle);
			adsClient.Disconnect();
		}
	}
}
