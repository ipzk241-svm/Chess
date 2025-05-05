using ChessGame.Classes;

namespace ChessGame
{
	public partial class MainForm : Form
	{
		private GameControler gameController;
		public MainForm()
		{
			InitializeComponent();
			BoardPanel board = new BoardPanel();
			ChessPanel.Controls.Add(board);
			var client = new NetworkClient("127.0.0.1", 5000, board);
			PlayerColor.Text = client.IsLocalPlayerWhite ? "White" : "Black";
			board.SetNetworkClient(client);

		}

		private void MainForm_Load(object sender, EventArgs e)
		{

		}
	}
}
