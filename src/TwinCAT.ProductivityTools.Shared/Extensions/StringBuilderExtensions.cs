using System.Text;

namespace TwinCAT.ProductivityTools.Extensions
{
	internal static class StringBuilderExtensions
	{
		internal static StringBuilder Preappend(
			this StringBuilder builder,
			char value,
			int repeatCount
		)
		{
			return builder.Insert(0, new string(value, repeatCount));
		}

		internal static StringBuilder Preappend(this StringBuilder builder, bool value)
		{
			return builder.Insert(0, value);
		}

		internal static StringBuilder Preappend(this StringBuilder builder, char value)
		{
			return builder.Insert(0, value);
		}

		internal static StringBuilder Preappend(this StringBuilder builder, ulong value)
		{
			return builder.Insert(0, value);
		}

		internal static StringBuilder Preappend(this StringBuilder builder, uint value)
		{
			return builder.Insert(0, value);
		}

		internal static StringBuilder Preappend(this StringBuilder builder, byte value)
		{
			return builder.Insert(0, value);
		}

		internal static StringBuilder Preappend(this StringBuilder builder, string value)
		{
			return builder.Insert(0, value);
		}

		internal static StringBuilder Preappend(this StringBuilder builder, float value)
		{
			return builder.Insert(0, value);
		}

		internal static StringBuilder Preappend(this StringBuilder builder, ushort value)
		{
			return builder.Insert(0, value);
		}

		internal static StringBuilder Preappend(this StringBuilder builder, object value)
		{
			return builder.Insert(0, value);
		}

		internal static StringBuilder Preappend(this StringBuilder builder, char[] value)
		{
			return builder.Insert(0, value);
		}

		internal static StringBuilder Preappend(this StringBuilder builder, sbyte value)
		{
			return builder.Insert(0, value);
		}

		internal static StringBuilder Preappend(this StringBuilder builder, decimal value)
		{
			return builder.Insert(0, value);
		}

		internal static StringBuilder Preappend(this StringBuilder builder, short value)
		{
			return builder.Insert(0, value);
		}

		internal static StringBuilder Preappend(this StringBuilder builder, int value)
		{
			return builder.Insert(0, value);
		}

		internal static StringBuilder Preappend(this StringBuilder builder, long value)
		{
			return builder.Insert(0, value);
		}

		internal static StringBuilder Preappend(this StringBuilder builder, double value)
		{
			return builder.Insert(0, value);
		}
	}
}
