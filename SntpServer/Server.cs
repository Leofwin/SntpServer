using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SntpServer
{
	public class Server
	{
		private readonly UdpClient udpClient;
		private readonly int deltaInMiliseconds;
		private IPEndPoint singleThreadClient = new IPEndPoint(IPAddress.Any, 0);

		private readonly ConcurrentQueue<(IPEndPoint, byte[])> requests;
		private readonly ConcurrentQueue<(IPEndPoint, byte[])> replies;


		public Server(IPAddress address, int port, string fileName)
		{
			udpClient = new UdpClient(new IPEndPoint(address, port));

			requests = new ConcurrentQueue<(IPEndPoint, byte[])>();
			replies = new ConcurrentQueue<(IPEndPoint, byte[])>();

			deltaInMiliseconds = int.Parse(File.ReadLines(fileName, Encoding.UTF8).ElementAt(0));
		}

		public void Start()
		{
			while (true)
			{
				Result.Of(() => udpClient.Receive(ref singleThreadClient))
					.Then(NtpMessage.ParseFromBytes)
					.Then(requestMesssage => NtpMessage.GenerateReplyFrom(requestMesssage, GetCurrentTime()))
					.Then(replyMessage => replyMessage.ToByteArray())
					.Then(b => udpClient.Send(b, b.Length, singleThreadClient))
					.OnFail(e => WriteError(e, singleThreadClient))
					.OnSuccess(value => WriteSuccess(singleThreadClient));
			}
		}

		public async void StartAsync()
		{
			while (true)
			{
				try
				{
					await ReceiveDataAsync();
					HandleRequest();
					await SendReply();
				}
				catch (Exception e)
				{
					Console.WriteLine($"FATAL {e.Message}");
				}
			}
		}

		private async Task ReceiveDataAsync()
		{
			var request = await udpClient.ReceiveAsync();
			requests.Enqueue((request.RemoteEndPoint, request.Buffer));
		}

		private void HandleRequest()
		{
			if (!requests.TryDequeue(out var requestTuple))
				return;

			var ntpRequest = NtpMessage.ParseFromBytes(requestTuple.Item2);
			var reply = NtpMessage.GenerateReplyFrom(ntpRequest.Value, GetCurrentTime());
			replies.Enqueue((requestTuple.Item1, reply.Value.ToByteArray()));
		}

		private async Task SendReply()
		{
			if (!replies.TryDequeue(out var tuple))
				return;

			await udpClient.SendAsync(tuple.Item2, tuple.Item2.Length, tuple.Item1);
		}

		private void WriteError(string errorMessage, IPEndPoint client)
		{
			Console.WriteLine($"ERROR [{client.Address}:{client.Port}] - {errorMessage}");
		}

		private void WriteSuccess(IPEndPoint client)
		{
			Console.WriteLine($"SUCCESS [{client.Address}:{client.Port}]");
		}

		private DateTime GetCurrentTime()
		{
			return DateTime.UtcNow + TimeSpan.FromMilliseconds(deltaInMiliseconds);
		}
	}
}
