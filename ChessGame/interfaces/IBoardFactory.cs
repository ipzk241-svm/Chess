using ChessGame.Classes.Pieces;
using ChessGame.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessGame.interfaces
{
	public interface IBoardFactory
	{
		Piece?[,] CreateInitialBoard(out Position whiteKingPos, out Position blackKingPos);
	}
}
