using ChessClassLibrary;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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

			string? join1 = await reader1.ReadLineAsync();
			string? join2 = await reader2.ReadLineAsync();

			if (join1 == null || join2 == null)
			{
				await writer1.WriteLineAsync("Opponent failed to join.");
				await writer2.WriteLineAsync("Opponent failed to join.");
				return;
			}

			string name1 = ExtractNameFromJoin(join1);
			string name2 = ExtractNameFromJoin(join2);

			await writer1.WriteLineAsync(JsonSerializer.Serialize(new CustomMessage
			{
				Type = MessageType.JOIN,
				Payload = name2
			}));

			await writer2.WriteLineAsync(JsonSerializer.Serialize(new CustomMessage
			{
				Type = MessageType.JOIN,
				Payload = name1
			}));

			var cts = new CancellationTokenSource();

			var task1 = ListenToPlayer(reader1, writer2, cts.Token);
			var task2 = ListenToPlayer(reader2, writer1, cts.Token);

			await Task.WhenAny(task1, task2);
			cts.Cancel();

			player1.Close();
			player2.Close();
		}

		private string ExtractNameFromJoin(string? json)
		{
			try
			{
				var msg = JsonSerializer.Deserialize<CustomMessage>(json);
				if (msg?.Type == MessageType.JOIN && !string.IsNullOrEmpty(msg.Payload))
					return msg.Payload;
			}
			catch { }

			return "Невідомий";
		}

		private async Task ListenToPlayer(StreamReader reader, StreamWriter opponentWriter, CancellationToken token)
		{
			try
			{
				while (!token.IsCancellationRequested)
				{
					string? msg = await reader.ReadLineAsync();
					if (msg == null)
					{
						await SendLeaveMessage(opponentWriter);
						break;
					}

					await opponentWriter.WriteLineAsync(msg);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Connection error: {ex.Message}");
				await SendLeaveMessage(opponentWriter);
			}
		}

		private async Task SendLeaveMessage(StreamWriter writer)
		{
			var leaveMsg = new CustomMessage
			{
				Type = MessageType.LEAVE,
				Payload = "Opponent"
			};

			string json = JsonSerializer.Serialize(leaveMsg);

			try
			{
				await writer.WriteLineAsync(json);
			}
			catch { }
		}
	}
}
