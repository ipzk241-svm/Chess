using ChessGame.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessGame.interfaces
{
	public interface IBoardPanel
	{
		void Invalidate();
		void SetMediator(IGameMediator mediator);
	}
}
