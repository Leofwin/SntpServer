using System;
using System.Linq;
using System.Text;

namespace SntpServer
{
	public enum LeapIndicator
	{
		NoWarning = 0,
		LastMinuteContains61Second = 1,
		LastMinuteContains59Second = 2,
		NotSyncrhonizied = 3
	}

	public class NtpMessage
	{
		private const int MinMessageSizeInBytes = 48;
		private const int MaxMessageSizeInBytes = 68;
		private const int RequestMode = 4;
		private static readonly DateTime StartTime = new DateTime(1900, 1, 1);

		#region Properties

		public LeapIndicator LeapIndicator { get; private set; }
		public int VersionNumber { get; private set; }
		public int Mode { get; private set; }
		public byte Stratum { get; private set; }
		public byte PollInterver { get; private set; }
		public byte Precision { get; private set; }
		public uint RootDelay { get; private set; }
		public uint RootDispersion { get; private set; }
		public uint ReferenceId { get; private set; }
		public TimeStamp ReferenceTimestamp { get; private set; }
		public TimeStamp OriginateTimestmap { get; private set; }
		public TimeStamp ReceiveTimestamp { get; private set; }
		public TimeStamp TransmitTimestamp { get; private set; }
		public uint Id { get; private set; }
		public string Digest { get; private set; }
		#endregion

		public static Result<NtpMessage> ParseFromBytes(byte[] bytes)
		{
			if (bytes.Length < MinMessageSizeInBytes || bytes.Length > MaxMessageSizeInBytes)
				return Result.Fail<NtpMessage>("Incorrect input data (should be more than 48 and less than 68)");

			var leapIndicator = (LeapIndicator)((bytes[0] & 0b11000000) >> 6);
			var versionNumber = (bytes[0] & 0b00111000) >> 3;
			var mode = bytes[0] & 0b00000111;

			var stratum = bytes[1];
			var pollInterver = bytes[2];
			var precision = bytes[3];

			var rootDelay = Converter.ToUInt32(bytes, 4);
			var rootDispersion = Converter.ToUInt32(bytes, 8);
			var referenceId = Converter.ToUInt32(bytes, 12);
			var referenceTimestamp = TimeStamp.ReadFromByteArray(bytes, 16);
			var originateTimestmap = TimeStamp.ReadFromByteArray(bytes, 24);
			var receiveTimestamp = TimeStamp.ReadFromByteArray(bytes, 32);
			var transmitTimestamp = TimeStamp.ReadFromByteArray(bytes, 40);
			var id = bytes.Length > 48 ? Converter.ToUInt32(bytes, 48) : 0;
			var digest = bytes.Length > 48 ? BitConverter.ToString(bytes, 52) : "";

			var result = new NtpMessage
			{
				LeapIndicator = leapIndicator,
				VersionNumber = versionNumber,
				Mode = mode,
				Stratum = stratum,
				PollInterver = pollInterver,
				Precision = precision,
				RootDelay = rootDelay,
				RootDispersion = rootDispersion,
				ReferenceId = referenceId,
				ReferenceTimestamp = referenceTimestamp,
				OriginateTimestmap = originateTimestmap,
				ReceiveTimestamp = receiveTimestamp,
				TransmitTimestamp = transmitTimestamp,
				Id = id,
				Digest = digest
			};

			return Result.Ok(result);
		}

		public static Result<NtpMessage> GenerateReplyFrom(NtpMessage ntpRequest, DateTime currentTime)
		{
			var receiveTimestamp = TimeStamp.FromDateTime(currentTime - StartTime);
			var result = new NtpMessage
			{
				LeapIndicator = LeapIndicator.NoWarning,
				VersionNumber = ntpRequest.VersionNumber,
				Mode = RequestMode,
				Stratum = 1,
				PollInterver = ntpRequest.PollInterver,
				Precision = 0xe9,
				RootDelay = 0,
				RootDispersion = 0,
				ReferenceId = BitConverter.ToUInt32(Encoding.UTF8.GetBytes("LOCL"), 0),
				ReceiveTimestamp = receiveTimestamp,
				OriginateTimestmap = ntpRequest.TransmitTimestamp,
				ReferenceTimestamp = receiveTimestamp,
				TransmitTimestamp = receiveTimestamp,
				Digest = ""
			};

			return Result.Ok(result);
		}

		public byte[] ToByteArray()
		{
			var result = new byte[MaxMessageSizeInBytes];

			result[0] = (byte)(((int)LeapIndicator << 6) + (VersionNumber << 3) + Mode);
			result[1] = Stratum;
			result[2] = PollInterver;
			result[3] = Precision;

			InsertInBytesArray(result, 4, RootDelay);
			InsertInBytesArray(result, 8, RootDispersion);
			InsertInBytesArray(result, 12, ReferenceId);
			InsertInBytesArray(result, 16, ReferenceTimestamp);
			InsertInBytesArray(result, 24, OriginateTimestmap);
			InsertInBytesArray(result, 32, ReceiveTimestamp);
			InsertInBytesArray(result, 40, TransmitTimestamp);
			InsertInBytesArray(result, 48, Id);
			InsertInBytesArray(result, 52, 16, Digest);

			return result;
		}

		private static void InsertInBytesArray(byte[] result, int startIndex, uint value)
		{
			var bytes = BitConverter.GetBytes(value);
			for (var i = 0; i < bytes.Length; i++)
				result[startIndex + i] = bytes[i];
		}

		private static void InsertInBytesArray(byte[] result, int startIndex, TimeStamp value)
		{
			var bytes = Converter.GetBytesFromTimeStamp(value);
			for (var i = 0; i < bytes.Length; i++)
				result[startIndex + i] = bytes[i];
		}

		private static void InsertInBytesArray(byte[] result, int startIndex, int length, string value)
		{
			var strToBytes = Encoding.UTF8.GetBytes(value);
			var zeros = Enumerable.Repeat(0, length - strToBytes.Length)
				.Select(z => (byte) z);

			var bytes = strToBytes.Concat(zeros).ToArray();

			for (var i = 0; i < length; i++)
				result[startIndex + i] = bytes[i];
		}
	}
}