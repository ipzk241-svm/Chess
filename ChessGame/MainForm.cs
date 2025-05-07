using ChessGame.classes;
using ChessGame.Classes;

namespace ChessGame
{
	public partial class MainForm : Form
	{
		private NetworkClient networkClient;
		private BoardPanel board;
		private readonly string userName;

		public MainForm(string userName)
		{
			InitializeComponent();
			this.userName = userName;
		}

		private async void MainForm_Load(object sender, EventArgs e)
		{
			loadingLabel.SafeInvoke(() => loadingLabel.Visible = true);
			board = new BoardPanel();

			try
			{
				await Task.Run(() =>
				{
					GameControler.Instance.StartGame();
					networkClient = new NetworkClient("127.0.0.1", 5000, board, userName);
				});
			}
			catch (Exception ex)
			{
				this.SafeInvoke(() =>
				{
					MessageBox.Show(ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					this.Close();
				});
				return;
			}

			this.SafeInvoke(() =>
			{
				ChessPanel.Controls.Add(board);
				board.SetNetworkClient(networkClient);
				InitializeLabels();
				SetupEventHandlers();
			});
		}

		private void InitializeLabels()
		{
			loadingLabel.Visible = false;
			playerName_label.Text = "Привіт гравцю " + userName;
			infoPanel.Visible = true;
		}

		private void SetupEventHandlers()
		{
			networkClient.OpponentNameReceived += name =>
			{
				oponentName_label.SafeInvoke(() =>
				{
					oponentName_label.Text = "Ти граєш проти " + name;
				});
			};

			GameControler.Instance.OnSideChanged += () =>
			{
				curSide_label.SafeInvoke(() =>
				{
					curSide_label.Text = GameControler.Instance.IsWhiteTurn ? "Білі" : "Чорні";
				});
			};

			networkClient.DisconnectAction += (name) =>
			{
				this.SafeInvoke(() =>
				{
					MessageBox.Show($"Суперник {name} вийшов із гри.", "Інформація", MessageBoxButtons.OK, MessageBoxIcon.Information);
					this.Close();
				});
			};
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			networkClient?.SendLeave();
			networkClient?.Disconnect();
		}
	}
}
