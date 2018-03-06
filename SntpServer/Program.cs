using System.Net;

namespace SntpServer
{
	class Program
	{
		static void Main(string[] args)
		{
			var server = new Server(IPAddress.Parse("127.0.0.1"), 123, "config.txt");
			server.Start();
		}
	}
}
