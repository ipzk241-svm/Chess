using ChessClassLibrary;
using ChessGame.Classes;
using ChessGame.Classes.Pieces;
using ChessGame.interfaces;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace ChessGame
{
	public class NetworkClient : INetworkClient
	{
		private TcpClient client;
		private StreamReader reader;
		private StreamWriter writer;
		private Thread listenThread;
		private Stream stream;
		public string ClientName { get; }
		private string _opponentName;

		public bool IsLocalPlayerWhite { get; set; }
		public event Action<string> OpponentNameReceived;
		public event Action<string> DisconnectAction;
		public event Action<Position, Position> MoveReceived;

		public NetworkClient(string host, int port, string clientName)
		{
			ClientName = clientName;
			ConnectToServer(host, port);
			SendJoinMessage();
			StartListening();
		}

		private void ConnectToServer(string host, int port)
		{
			try
			{
				client = new TcpClient(host, port);
				stream = client.GetStream();
				reader = new StreamReader(stream);
				writer = new StreamWriter(stream) { AutoFlush = true };

				string role = reader.ReadLine();
				IsLocalPlayerWhite = role == "white";
			}
			catch (Exception ex)
			{
				throw new Exception("Не вдалося підключитись до сервера.", ex);
			}
		}

		private void SendJoinMessage()
		{
			var msg = new CustomMessage
			{
				Type = MessageType.JOIN,
				Payload = ClientName
			};
			string json = JsonSerializer.Serialize(msg);
			writer.WriteLine(json);
		}

		private void StartListening()
		{
			listenThread = new Thread(ListenForMoves);
			listenThread.IsBackground = true;
			listenThread.Start();
		}

		private async void ListenForMoves()
		{
			try
			{
				while (true)
				{
					string json = await reader.ReadLineAsync();
					if (json == null) break;

					var message = JsonSerializer.Deserialize<CustomMessage>(json);
					if (message == null) continue;

					HandleIncomingMessage(message);
				}
			}
			catch
			{
				DisconnectAction?.Invoke(_opponentName);
			}
		}

		public void SendMove(Position from, Position to)
		{
			var msg = new CustomMessage
			{
				Type = MessageType.MOVE,
				Payload = $"{from.X},{from.Y}:{to.X},{to.Y}"
			};
			string json = JsonSerializer.Serialize(msg);
			writer.WriteLineAsync(json).GetAwaiter().GetResult();
		}

		private void HandleIncomingMessage(CustomMessage msg)
		{
			switch (msg.Type)
			{
				case MessageType.JOIN:
					_opponentName = msg.Payload;
					OpponentNameReceived?.Invoke(_opponentName);
					break;

				case MessageType.MOVE:
					HandleMove(msg.Payload);
					break;

				case MessageType.LEAVE:
					DisconnectAction?.Invoke(_opponentName);
					break;
			}
		}

		private void HandleMove(string move)
		{
			try
			{
				var parts = move.Split(':');
				var from = parts[0].Split(',');
				var to = parts[1].Split(',');

				var start = new Position(int.Parse(from[0]), int.Parse(from[1]));
				var end = new Position(int.Parse(to[0]), int.Parse(to[1]));

				MoveReceived?.Invoke(start, end);
			}
			catch
			{
				MessageBox.Show("Помилка при обробці ходу.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		public void SendLeave()
		{
			if (writer == null) return;

			var msg = new CustomMessage
			{
				Type = MessageType.LEAVE,
				Payload = ClientName
			};

			string json = JsonSerializer.Serialize(msg);
			writer.WriteLineAsync(json).GetAwaiter().GetResult();
		}

		public void Disconnect()
		{
			stream?.Close();
			client?.Close();
		}
	}
}