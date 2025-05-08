using System;
using System.Windows.Forms;

namespace ChessGame
{
	public partial class EntryForm : Form
	{
		public EntryForm()
		{
			InitializeComponent();
		}

		private void btn_FindGame_Click(object sender, EventArgs e)
		{
			string userName = tb_name.Text.Trim();
			if (string.IsNullOrEmpty(userName))
			{
				MessageBox.Show("Введіть ім'я гравця.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			this.Hide();
			MainForm gameForm = new MainForm(userName);
			gameForm.FormClosed += (s, args) =>
			{
				this.Show();
			};
			gameForm.Show();
		}
	}
}