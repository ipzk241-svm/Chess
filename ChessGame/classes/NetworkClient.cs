using ChessClassLibrary;
using ChessGame.Classes;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Windows.Forms;
using System.Threading;
using ChessGame.classes;

namespace ChessGame
{
	public class NetworkClient
	{
		private TcpClient client;
		private StreamReader reader;
		private StreamWriter writer;
		private Thread listenThread;
		private BoardPanel boardPanel;
		private Stream stream;
		public string clientName;
		private string _opponentName;
		public string opponentName
		{
			get => _opponentName;
			set
			{
				_opponentName = value;
				OpponentNameReceived?.Invoke(_opponentName);
			}
		}

		public bool IsLocalPlayerWhite { get; set; }

		public event Action<string> OpponentNameReceived;
		public event Action<string> DisconnectAction;

		public NetworkClient(string host, int port, BoardPanel panel, string clientName)
		{
			this.boardPanel = panel;
			this.clientName = clientName;

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
				Payload = clientName
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
				boardPanel.SafeInvoke(() => DisconnectAction?.Invoke(opponentName));
			}
		}

		public async void SendMove(Position from, Position to)
		{
			var msg = new CustomMessage
			{
				Type = MessageType.MOVE,
				Payload = $"{from.X},{from.Y}:{to.X},{to.Y}"
			};
			string json = JsonSerializer.Serialize(msg);
			await writer.WriteLineAsync(json);
		}

		private void HandleIncomingMessage(CustomMessage msg)
		{
			switch (msg.Type)
			{
				case MessageType.JOIN:
					opponentName = msg.Payload;
					break;

				case MessageType.MOVE:
					HandleMove(msg.Payload);
					break;

				case MessageType.LEAVE:
					boardPanel.SafeInvoke(() =>
					{
						DisconnectAction?.Invoke(opponentName);
					});
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

				GameControler.Instance.MovePieceFromNetwork(start, end);

				boardPanel.SafeInvoke(() => boardPanel.Invalidate());
			}
			catch
			{
				boardPanel.SafeInvoke(() =>
				{
					MessageBox.Show("Помилка при обробці ходу.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
				});
			}
		}

		public async void SendLeave()
		{
			if (writer == null) return;

			var msg = new CustomMessage
			{
				Type = MessageType.LEAVE,
				Payload = clientName
			};

			string json = JsonSerializer.Serialize(msg);
			await writer.WriteLineAsync(json);
		}

		public void Disconnect()
		{
			stream?.Close();
			client?.Close();
		}
	}
}
