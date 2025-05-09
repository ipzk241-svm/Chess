using System.Net.Sockets;
using System.Text.Json;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChessClassLibrary;
using ChessServer;

public class GameSession
{
	private readonly Logger logger;
	private TcpClient player1;
	private TcpClient player2;

	private readonly List<MoveRecord> moveHistory = new();
	private string playerWhite = "Білий";
	private string playerBlack = "Чорний";

	public GameSession(TcpClient p1, TcpClient p2, Logger logger)
	{
		player1 = p1;
		player2 = p2;
		this.logger = logger;
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
			logger.Warning("Opponent failed to join.");
			return;
		}

		playerWhite = ExtractNameFromJoin(join1);
		playerBlack = ExtractNameFromJoin(join2);

		logger.Info($"Game started: {playerWhite} (white) vs {playerBlack} (black)");

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

		logger.Info($"Game session ended: {playerWhite} vs {playerBlack}");

		player1.Close();
		player2.Close();
	}

	private string ExtractNameFromJoin(string joinMessage)
	{
		try
		{
			var message = JsonSerializer.Deserialize<CustomMessage>(joinMessage);
			return message?.Payload ?? "Unknown";
		}
		catch (Exception ex)
		{
			logger.Error("Failed to extract name from join message: " + ex.Message);
			return "Unknown";
		}
	}

	private async Task ListenToPlayer(StreamReader reader, StreamWriter opponentWriter, bool isWhite, CancellationToken token)
	{
		string playerColor = isWhite ? "White" : "Black";

		try
		{
			while (!token.IsCancellationRequested)
			{
				var line = await reader.ReadLineAsync();
				if (line == null) break;

				var message = JsonSerializer.Deserialize<CustomMessage>(line);
				if (message == null) continue;

				if (message.Type == MessageType.MOVE)
				{
					ParseMove(message.Payload, playerColor);
				}

				await opponentWriter.WriteLineAsync(line);
			}
		}
		catch (Exception ex)
		{
			logger.Error($"{playerColor} player error: {ex.Message}");
		}
		finally
		{
			await SendLeaveMessage(opponentWriter);
			logger.Warning($"{playerColor} player disconnected.");
		}
	}

	private async Task SendLeaveMessage(StreamWriter writer)
	{
		try
		{
			var message = new CustomMessage { Type = MessageType.LEAVE, Payload = "" };
			await writer.WriteLineAsync(JsonSerializer.Serialize(message));
		}
		catch (Exception ex)
		{
			logger.Error("Failed to send LEAVE message: " + ex.Message);
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
			(
				int.Parse(from[0]),
				int.Parse(from[1])
			);
			var end = new Position
			(
				int.Parse(to[0]),
				int.Parse(to[1])
			);

			moveHistory.Add(new MoveRecord
			{
				From = start,
				To = end,
				Timestamp = DateTime.UtcNow,
				PlayerColor = playerColor
			});

			logger.Info($"{playerColor} move: {start.X},{start.Y} → {end.X},{end.Y}");
		}
		catch (Exception ex)
		{
			logger.Error("Error parsing move: " + ex.Message);
		}
	}

	private async Task SaveGameHistoryAsync()
	{
		try
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

			logger.Info($"Game history saved: {path}");
		}
		catch (Exception ex)
		{
			logger.Error("Error saving game history: " + ex.Message);
		}
	}
}
