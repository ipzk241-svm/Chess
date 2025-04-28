using ChessGame.Classes;

namespace ChessGame
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
			BoardPanel board = new BoardPanel();
			ChessPanel.Controls.Add(board);
			//Controls.Add(board);
		}

		private void MainForm_Load(object sender, EventArgs e)
		{

		}
	}
}
