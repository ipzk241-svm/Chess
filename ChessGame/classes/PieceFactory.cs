using ChessGame.Classes.Pieces;
using ChessGame.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessGame.Classes
{
	public class PieceFactory : IPieceFactory
	{
		public Piece CreatePiece(PieceType type, Position pos, bool isWhite)
		{
			return type switch
			{
				PieceType.Pawn => new Pawn(pos, isWhite),
				PieceType.Rook => new Rook(pos, isWhite),
				PieceType.Knight => new Knight(pos, isWhite),
				PieceType.Bishop => new Bishop(pos, isWhite),
				PieceType.Queen => new Queen(pos, isWhite),
				PieceType.King => new King(pos, isWhite),
				_ => throw new ArgumentException("Невідомий тип фігури")
			};
		}
	}
}
