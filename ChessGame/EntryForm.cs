using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
			var userName = tb_name.Text;
			this.Hide();

			MainForm gameForm = new MainForm(userName);
			gameForm.ShowDialog();

			this.Show(); 
		}

	}
}
