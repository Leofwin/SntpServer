using System;

namespace SntpServer
{
	public class TimeStamp
	{
		private const uint milisecondsCoef = 0x3E800000;
		public readonly uint Integer;
		public readonly uint Fraction;

		public TimeStamp(uint integer, uint fraction)
		{
			Integer = integer;
			Fraction = fraction;
		}

		public ulong GetTotalMilliseconds()
		{
			var fraction = (Fraction * milisecondsCoef) >> 20;

			return (Integer << 12) + fraction;
		}

		public static TimeStamp ReadFromByteArray(byte[] bytes, int index)
		{
			if (index < 0 || index > bytes.Length - 8)
				throw new ArgumentException("Incorrect index");

			return new TimeStamp(
				Converter.ToUInt32(bytes, index), 
				Converter.ToUInt32(bytes, index + 4)
				);
		}
	
		public static TimeStamp FromDateTime(TimeSpan time)
		{
			var integer = (uint) time.TotalSeconds;

			var bitsCount = GetBitsCount(time.Milliseconds);
			var fraction = (uint)time.Milliseconds << (32 - bitsCount);

			return new TimeStamp(integer, fraction);
		}

		private static int GetBitsCount(int timeMilliseconds)
		{
			var count = 0;
			while (timeMilliseconds > 0)
			{
				timeMilliseconds = timeMilliseconds >> 1;
				count++;
			}

			return count;
		}

		public static explicit operator ulong(TimeStamp value)
		{
			return ((ulong)value.Integer << 32) + value.Fraction;
		}
	}
}
