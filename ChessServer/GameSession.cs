using System;
using System.IO;
using System.Net.Sockets;

namespace ChessServer
{

	public class GameSession
	{
		private TcpClient player1;
		private TcpClient player2;

		public GameSession(TcpClient p1, TcpClient p2)
		{
			player1 = p1;
			player2 = p2;
		}

		public void Run()
		{
			using var stream1 = player1.GetStream();
			using var stream2 = player2.GetStream();
			using var reader1 = new StreamReader(stream1);
			using var writer1 = new StreamWriter(stream1) { AutoFlush = true };
			using var reader2 = new StreamReader(stream2);
			using var writer2 = new StreamWriter(stream2) { AutoFlush = true };

			writer1.WriteLine("You are Player 1");
			writer2.WriteLine("You are Player 2");

			bool running = true;
			while (running)
			{
				try
				{
					if (stream1.DataAvailable)
					{
						string msg1 = reader1.ReadLine();
						writer2.WriteLine(msg1);
					}

					if (stream2.DataAvailable)
					{
						string msg2 = reader2.ReadLine();
						writer1.WriteLine(msg2);
					}
				}
				catch
				{
					Console.WriteLine("One of the players disconnected.");
					break;
				}
			}

			player1.Close();
			player2.Close();
		}
	}

}
