using ChessClassLibrary;
using ChessGame.Classes;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

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
		public string opponentName;
		public bool IsLocalPlayerWhite { get; set; }

		public event Action<string> OpponentNameReceived;

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
			client = new TcpClient(host, port);
			stream = client.GetStream();
			reader = new StreamReader(stream);
			writer = new StreamWriter(stream) { AutoFlush = true };

			string role = reader.ReadLine();
			IsLocalPlayerWhite = role == "white";
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
			listenThread.Start();
		}


		private async void ListenForMoves()
		{
			try
			{
				while (true)
				{
					string json = await reader.ReadLineAsync();
					var message = JsonSerializer.Deserialize<CustomMessage>(json);
					if (message == null) break;

					Application.OpenForms[0]?.BeginInvoke(new Action(() =>
					{
						HandleIncomingMessage(message);
					}));
				}
			}
			catch
			{
				MessageBox.Show("Disconnected from server.");
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
			if(msg.Type == MessageType.JOIN)
			{
				opponentName = msg.Payload;
				OpponentNameReceived?.Invoke(opponentName);
			}
			if (msg.Type == MessageType.MOVE)
			{
				HandleMove(msg.Payload);
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
				boardPanel.Invalidate();
			}
			catch
			{
				MessageBox.Show("Received malformed move from other player.");
			}
		}
		public void Disconnect()
		{
			stream?.Close();
			client?.Close();
		}
	}

}