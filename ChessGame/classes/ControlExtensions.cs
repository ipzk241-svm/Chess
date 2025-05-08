using System;
using System.Windows.Forms;
namespace ChessGame.Classes
{
	public static class ControlExtensions
	{
		public static void SafeInvoke(this Control control, Action action)
		{
			if (control == null || control.IsDisposed || !control.IsHandleCreated)
				return;

			if (control.InvokeRequired)
			{
				control.Invoke(action);
			}
			else
			{
				action();
			}
		}
	}

}
