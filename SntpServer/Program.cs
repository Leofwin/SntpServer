using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace SntpServer
{
	class Program
	{
		static void Main(string[] args)
		{
			Server server;
			try
			{
				server = new Server(IPAddress.Parse("127.0.0.1"), 123, "config.txt");
			}
			catch (SocketException e)
			{
				Console.WriteLine("Can't bind 123 port");
				return;
			}
			catch (FileNotFoundException e)
			{
				Console.WriteLine("Can't find config file");
				return;
			}

			server.StartAsync();
//			server.Start();
			Console.ReadKey();
		}
	}
}
