using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using ChessGame.Classes;
using ChessGame.Classes.Pieces;

namespace ChessGame
{
	using System;
	using System.IO;
	using System.Net.Sockets;
	using System.Threading;
	using System.Windows.Forms;

	public class NetworkClient
	{
		private TcpClient client;
		private StreamReader reader;
		private StreamWriter writer;
		private Thread listenThread;
		private BoardPanel boardPanel;
		public bool IsLocalPlayerWhite { get; set; }

		public NetworkClient(string host, int port, BoardPanel panel)
		{
			boardPanel = panel;
			client = new TcpClient(host, port);
			var stream = client.GetStream();
			reader = new StreamReader(stream);
			string role = reader.ReadLine();
			IsLocalPlayerWhite = role == "white" ? true : false;
			writer = new StreamWriter(stream) { AutoFlush = true };

			listenThread = new Thread(ListenForMoves);
			listenThread.Start();
		}

		private void ListenForMoves()
		{
			try
			{
				while (true)
				{
					string message = reader.ReadLine();
					if (message == null) break;

					Application.OpenForms[0]?.BeginInvoke(new Action(() =>
					{
						HandleIncomingMove(message);
					}));
				}
			}
			catch
			{
				MessageBox.Show("Disconnected from server.");
			}
		}

		public void SendMove(Position from, Position to)
		{
			writer.WriteLine($"{from.X},{from.Y}:{to.X},{to.Y}");
		}

		private void HandleIncomingMove(string msg)
		{
			try
			{
				var parts = msg.Split(':');
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
	}

}