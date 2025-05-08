using ChessGame.Classes;
using ChessGame.interfaces;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace ChessGame
{
	public partial class MainForm : Form
	{
		private IGameMediator _mediator;
		private readonly string _userName;
		private BoardPanel _boardPanel;
		private NetworkClient _networkClient;
		private bool _connected = false;
		private CancellationTokenSource _cts = new();


		public MainForm(string userName)
		{
			InitializeComponent();
			_userName = userName;
			_boardPanel = new BoardPanel();
		}

		private async void MainForm_Load(object sender, EventArgs e)
		{
			loadingLabel.SafeInvoke(() => loadingLabel.Visible = true);
			AnimateLoadingLabel();

			try
			{
				await Task.Run(() =>
				{
					GameControler.Instance.StartGame();

					if (_cts.Token.IsCancellationRequested) return;

					_networkClient = new NetworkClient("127.0.0.1", 5000, _userName);

					if (_cts.Token.IsCancellationRequested)
					{
						_networkClient.Disconnect(); // підключився, але форму вже закрили
						return;
					}

					_mediator = new GameMediator(GameControler.Instance, _boardPanel, _networkClient, () => this.SafeInvoke(Close));
					_connected = true;
				}, _cts.Token);

				this.SafeInvoke(() =>
				{
					ChessPanel.Controls.Add(_boardPanel);
					SetupEventHandlers();
					InitializeLabels();
				});
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex)
			{
				this.SafeInvoke(() =>
				{
					Console.WriteLine($"Connection error: {ex.Message}");
					MessageBox.Show(ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					this.Close();
				});
			}
		}



		private void InitializeLabels()
		{
			loadingLabel.Visible = false;
			playerName_label.Text = "Привіт, " + _userName;
			infoPanel.Visible = true;
		}
		private async void AnimateLoadingLabel()
		{
			while (loadingLabel.Visible)
			{
				for (int i = 0; i <= 3; i++)
				{
					loadingLabel.SafeInvoke(() => loadingLabel.Text = "Пошук гравця" + new string('.', i));
					await Task.Delay(500);
				}
			}
		}
		private void SetupEventHandlers()
		{
			_networkClient.OpponentNameReceived += name =>
			{
				loadingLabel.Visible = false;
				oponentName_label.Text = "Ти граєш проти " + name;
			};

			GameControler.Instance.OnSideChanged += () =>
			{
				curSide_label.SafeInvoke(() =>
				{
					curSide_label.Text = GameControler.Instance.IsWhiteTurn ? "Білі" : "Чорні";
				});
			};

		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			_cts.Cancel();

			if (_mediator != null)
				_mediator.Disconnect();
			else if (_connected && _networkClient != null)
				_networkClient.Disconnect();
		}


	}
}