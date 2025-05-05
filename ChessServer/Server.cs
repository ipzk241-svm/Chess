using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ChessServer
{


	public class Server
	{
		private TcpListener listener;
		private ConcurrentQueue<TcpClient> waitingClients = new();
		private bool isRunning = true;

		public void Start(int port)
		{
			listener = new TcpListener(IPAddress.Any, port);
			listener.Start();
			Console.WriteLine($"Server started on port {port}.");

			while (isRunning)
			{
				TcpClient client = listener.AcceptTcpClient();
				Console.WriteLine("New client connected.");

				waitingClients.Enqueue(client);
				TryStartGame();
			}
		}

		private void TryStartGame()
		{
			if (waitingClients.Count >= 2)
			{
				if (waitingClients.TryDequeue(out var client1) &&
					waitingClients.TryDequeue(out var client2))
				{
					Console.WriteLine("Starting new game session.");
					var session = new GameSession(client1, client2);
					var thread = new Thread(session.Run);
					thread.Start();
				}
			}
		}
	}

}
