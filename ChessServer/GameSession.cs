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

		public async Task RunAsync()
		{
			using var stream1 = player1.GetStream();
			using var stream2 = player2.GetStream();
			using var reader1 = new StreamReader(stream1);
			using var writer1 = new StreamWriter(stream1) { AutoFlush = true };
			using var reader2 = new StreamReader(stream2);
			using var writer2 = new StreamWriter(stream2) { AutoFlush = true };

			await writer1.WriteLineAsync("white");
			await writer2.WriteLineAsync("black");

			bool isWhiteTurn = true;
			bool running = true;

			while (running)
			{
				try
				{
					if (isWhiteTurn)
					{
						string? msg = await reader1.ReadLineAsync();
						if (msg == null) break;

						await writer2.WriteLineAsync(msg);
						isWhiteTurn = false;
					}
					else
					{
						string? msg = await reader2.ReadLineAsync();
						if (msg == null) break;

						await writer1.WriteLineAsync(msg);
						isWhiteTurn = true;
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Connection error: {ex.Message}");
					break;
				}
			}

			player1.Close();
			player2.Close();
		}

	}

}
