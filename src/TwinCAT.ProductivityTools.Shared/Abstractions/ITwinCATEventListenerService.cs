using System;
using System.Collections.Generic;
using System.Text;
using TwinCAT.Ads;
using TwinCAT.ProductivityTools.Services;

namespace TwinCAT.ProductivityTools.Abstractions
{
	public delegate void MessageOccuredEventHandler(object sender, MessageOccuredEventArgs e);

	internal interface ITwinCATEventListenerService
	{
		void Connect(AmsNetId target);
		void Disconnect();

		event MessageOccuredEventHandler MessageOccured;
	}
}
