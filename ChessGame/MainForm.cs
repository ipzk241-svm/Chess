using ChessGame.Classes;

namespace ChessGame
{
	public partial class MainForm : Form
	{
		private NetworkClient networkClient;
		public MainForm(string userName)
		{
			InitializeComponent();
			InitializeGame(userName);
			InitializeLabels(userName);
			SetupEventHandlers();
		}

		private void InitializeGame(string userName)
		{
			BoardPanel board = new BoardPanel();
			GameControler.Instance.StartGame();
			ChessPanel.Controls.Add(board);
			networkClient = new NetworkClient("127.0.0.1", 5000, board, userName);
			board.SetNetworkClient(networkClient);
		}

		private void InitializeLabels(string userName)
		{
			playerName_label.Text = "Привіт гравцю " + userName;
		}

		private void SetupEventHandlers()
		{
			networkClient.OpponentNameReceived += name =>
			{
				if (InvokeRequired)
					BeginInvoke(() => oponentName_label.Text = "Ти граєш проти " + name);
				else
					oponentName_label.Text = "Ти граєш проти " + name;
			};

			GameControler.Instance.OnSideChanged += () =>
			{
				curSide_label.Text = GameControler.Instance.IsWhiteTurn ? "Білі" : "Чорні";
			};
		}


		private void MainForm_Load(object sender, EventArgs e)
		{

		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			networkClient?.Disconnect();
		}
	}
}
