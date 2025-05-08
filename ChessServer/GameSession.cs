using ChessClassLibrary;
using System;
using System.Collections.Generic;
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

		private readonly List<MoveRecord> moveHistory = new();
		private string playerWhite = "Білий";
		private string playerBlack = "Чорний";

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

			playerWhite = ExtractNameFromJoin(join1);
			playerBlack = ExtractNameFromJoin(join2);

			await writer1.WriteLineAsync(JsonSerializer.Serialize(new CustomMessage
			{
				Type = MessageType.JOIN,
				Payload = playerBlack
			}));

			await writer2.WriteLineAsync(JsonSerializer.Serialize(new CustomMessage
			{
				Type = MessageType.JOIN,
				Payload = playerWhite
			}));

			var cts = new CancellationTokenSource();

			var task1 = ListenToPlayer(reader1, writer2, true, cts.Token);
			var task2 = ListenToPlayer(reader2, writer1, false, cts.Token);

			await Task.WhenAny(task1, task2);
			cts.Cancel();

			await SaveGameHistoryAsync();

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

		private async Task ListenToPlayer(StreamReader reader, StreamWriter opponentWriter, bool isWhite, CancellationToken token)
		{
			try
			{
				string color = isWhite ? "white" : "black";

				while (!token.IsCancellationRequested)
				{
					string? msg = await reader.ReadLineAsync();
					if (msg == null)
					{
						await SendLeaveMessage(opponentWriter);
						break;
					}

					var deserialized = JsonSerializer.Deserialize<CustomMessage>(msg);
					if (deserialized != null && deserialized.Type == MessageType.MOVE)
					{
						ParseMove(deserialized.Payload, color);
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

		private void ParseMove(string move, string playerColor)
		{
			try
			{
				var parts = move.Split(':');
				var from = parts[0].Split(',');
				var to = parts[1].Split(',');

				var start = new Position
				{
					X = int.Parse(from[0]),
					Y = int.Parse(from[1])
				};
				var end = new Position
				{
					X = int.Parse(to[0]),
					Y = int.Parse(to[1])
				};

				moveHistory.Add(new MoveRecord
				{
					From = start,
					To = end,
					Timestamp = DateTime.UtcNow,
					PlayerColor = playerColor
				});
			}
			catch
			{
				Console.WriteLine("Помилка при збереженні ходу.");
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

		private async Task SaveGameHistoryAsync()
		{
			var record = new GameHistoryRecord
			{
				PlayerWhite = playerWhite,
				PlayerBlack = playerBlack,
				Moves = moveHistory
			};

			var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameHistory");
			Directory.CreateDirectory(dir);

			string fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{playerWhite}_vs_{playerBlack}.json";
			string path = Path.Combine(dir, fileName);

			string json = JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true });

			await File.WriteAllTextAsync(path, json);
			Console.WriteLine($"Game saved to: {path}");
		}
	}
}
