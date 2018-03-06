using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SntpServer
{
	public class Server
	{
		private readonly UdpClient udpClient;
		private IPEndPoint client;
		private readonly int deltaInMiliseconds;

		public Server(IPAddress address, int port, string fileName)
		{
			udpClient = new UdpClient(new IPEndPoint(address, port));
			client = new IPEndPoint(IPAddress.Any, 0);

			deltaInMiliseconds = int.Parse(File.ReadLines(fileName).ElementAt(0));
		}

		public void Start()
		{
			while (true)
			{
				Result.Of(() => udpClient.Receive(ref client))
					.Then(NtpMessage.ParseFromBytes)
					.Then(requestMesssage => NtpMessage.GenerateReplyFrom(requestMesssage, GetCurrentTime()))
					.Then(replyMessage => replyMessage.ToByteArray())
					.Then(bytes => udpClient.Send(bytes, bytes.Length, client))
					.OnFail(errorMessage => Console.WriteLine(
						$"ERROR [{client.Address}:{client.Port}] - {errorMessage}")
					)
					.OnSuccess(value => Console.WriteLine($"SUCCESS [{client.Address}:{client.Port}]"));
			}
		}

		private DateTime GetCurrentTime()
		{
			return DateTime.UtcNow + TimeSpan.FromMilliseconds(deltaInMiliseconds);
		}
	}
}
