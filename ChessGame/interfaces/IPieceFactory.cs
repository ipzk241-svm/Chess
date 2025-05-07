using ChessGame.Classes.Pieces;
using ChessGame.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessGame.interfaces
{
	public interface IPieceFactory
	{
		Piece CreatePiece(PieceType type, Position pos, bool isWhite);
	}
}
