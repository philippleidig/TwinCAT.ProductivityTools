using System;
using System.IO;
using System.Text;
using FluentAssertions;
using TwinCAT.ProductivityTools.DataTypes;
using TwinCAT.ProductivityTools.Events;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Events
{
	public class BinaryEventMessageParserTests
	{
		private readonly BinaryEventMessageParser parser = new BinaryEventMessageParser();

		[Fact]
		public void Reads_every_field_of_a_notification()
		{
			var raised = new DateTime(2024, 5, 17, 8, 30, 15, DateTimeKind.Local);

			EventMessage message = parser.Parse(
				Notification(raised, EventSeverity.WARN, 851, "TCNC", "Axis 1 not ready")
			);

			message.TimeRaised.Should().Be(raised);
			message.Severity.Should().Be(EventSeverity.WARN);
			message.AdsPort.Should().Be(851);
			message.Sender.Should().Be("TCNC");
			message.Message.Should().Be("Axis 1 not ready");
		}

		[Theory]
		[InlineData(EventSeverity.HINT)]
		[InlineData(EventSeverity.WARN)]
		[InlineData(EventSeverity.ERROR)]
		[InlineData(EventSeverity.LOG)]
		public void Maps_the_severity_flags(EventSeverity severity)
		{
			parser.Parse(Notification(DateTime.Now, severity, 1, "S", "m")).Severity.Should().Be(severity);
		}

		[Fact]
		public void Stops_the_sender_at_its_terminator_instead_of_padding_it()
		{
			parser
				.Parse(Notification(DateTime.Now, EventSeverity.LOG, 1, "TCRTIME", "m"))
				.Sender.Should()
				.Be("TCRTIME");
		}

		[Fact]
		public void Decodes_non_ascii_characters_as_utf8()
		{
			parser
				.Parse(Notification(DateTime.Now, EventSeverity.LOG, 1, "S", "Überlauf"))
				.Message.Should()
				.Be("Überlauf");
		}

		[Fact]
		public void Returns_an_empty_message_when_the_payload_carries_none()
		{
			parser
				.Parse(Notification(DateTime.Now, EventSeverity.LOG, 1, "S", string.Empty))
				.Message.Should()
				.BeEmpty();
		}

		[Fact]
		public void Truncates_a_message_that_claims_to_be_longer_than_the_payload()
		{
			byte[] payload = Notification(DateTime.Now, EventSeverity.LOG, 1, "S", "abc");

			// Claim four times the length that is actually there.
			BitConverter.GetBytes(12).CopyTo(payload, BinaryEventMessageParser.HeaderLength - 4);

			parser.Parse(payload).Message.Should().Be("abc");
		}

		[Fact]
		public void Ignores_a_negative_message_length()
		{
			byte[] payload = Notification(DateTime.Now, EventSeverity.LOG, 1, "S", "abc");

			BitConverter.GetBytes(-1).CopyTo(payload, BinaryEventMessageParser.HeaderLength - 4);

			parser.Parse(payload).Message.Should().BeEmpty();
		}

		[Fact]
		public void Falls_back_to_the_minimum_date_when_the_timestamp_is_not_a_file_time()
		{
			byte[] payload = Notification(DateTime.Now, EventSeverity.LOG, 1, "S", "m");

			BitConverter.GetBytes(long.MaxValue).CopyTo(payload, 0);

			parser.Parse(payload).TimeRaised.Should().Be(DateTime.MinValue);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(4)]
		[InlineData(BinaryEventMessageParser.HeaderLength - 1)]
		public void Returns_null_for_a_payload_that_is_too_short(int length)
		{
			parser.Parse(new byte[length]).Should().BeNull();
		}

		[Fact]
		public void Returns_null_for_a_missing_payload()
		{
			parser.Parse(null).Should().BeNull();
		}

		private static byte[] Notification(
			DateTime raised,
			EventSeverity severity,
			int adsPort,
			string sender,
			string message
		)
		{
			using (var stream = new MemoryStream())
			using (var writer = new BinaryWriter(stream))
			{
				writer.Write(raised.ToFileTime());
				writer.Write((int)severity);
				writer.Write(adsPort);
				writer.Write(FixedLength(sender, BinaryEventMessageParser.SenderLength));

				byte[] body = Encoding.UTF8.GetBytes(message);

				writer.Write(body.Length);
				writer.Write(body);
				writer.Flush();

				return stream.ToArray();
			}
		}

		private static byte[] FixedLength(string value, int length)
		{
			var buffer = new byte[length];

			Encoding.UTF8.GetBytes(value).CopyTo(buffer, 0);

			return buffer;
		}
	}
}
