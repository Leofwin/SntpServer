namespace SntpServer
{
	public static class Converter
	{
		private const int BitsInByte = 8;
		public static uint ToUInt32(byte[] bytes, int startIndex)
		{
			const int bytesCount = 4;
			uint result = 0;
			for (var i = 0; i < bytesCount; i++)
				result += (uint)bytes[startIndex + i] << ((bytesCount - 1 - i) * BitsInByte);

			return result;
		}

		public static byte[] GetBytesFromTimeStamp(TimeStamp timeStamp)
		{
			var value = (ulong) timeStamp;
			var result = new byte[8];
			var mask = (ulong)0xFF;

			for (var i = result.Length - 1; i >= 0; i--)
			{
				var temp = (value & mask) >> (result.Length - i - 1) * BitsInByte;
				result[i] = (byte)temp;
				mask = mask << BitsInByte;
			}

			return result;
		}
	}
}
