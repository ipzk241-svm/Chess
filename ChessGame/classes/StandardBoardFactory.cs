using ChessGame.Classes.Pieces;
using ChessGame.Classes;
using ChessGame.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessGame.classes
{
	public class StandardBoardFactory : IBoardFactory
	{
		private readonly IPieceFactory _pieceFactory;

		public StandardBoardFactory(IPieceFactory pieceFactory)
		{
			_pieceFactory = pieceFactory;
		}

		public Piece?[,] CreateInitialBoard(out Position whiteKingPos, out Position blackKingPos)
		{
			var board = new Piece[8, 8];

			for (int i = 0; i < 8; i++)
			{
				board[1, i] = _pieceFactory.CreatePiece(PieceType.Pawn, new Position(i, 1), false);
				board[6, i] = _pieceFactory.CreatePiece(PieceType.Pawn, new Position(i, 6), true);
			}

			board[0, 0] = _pieceFactory.CreatePiece(PieceType.Rook, new Position(0, 0), false);
			board[0, 1] = _pieceFactory.CreatePiece(PieceType.Knight, new Position(1, 0), false);
			board[0, 2] = _pieceFactory.CreatePiece(PieceType.Bishop, new Position(2, 0), false);
			board[0, 3] = _pieceFactory.CreatePiece(PieceType.Queen, new Position(3, 0), false);
			board[0, 4] = _pieceFactory.CreatePiece(PieceType.King, new Position(4, 0), false);
			board[0, 5] = _pieceFactory.CreatePiece(PieceType.Bishop, new Position(5, 0), false);
			board[0, 6] = _pieceFactory.CreatePiece(PieceType.Knight, new Position(6, 0), false);
			board[0, 7] = _pieceFactory.CreatePiece(PieceType.Rook, new Position(7, 0), false);

			board[7, 0] = _pieceFactory.CreatePiece(PieceType.Rook, new Position(0, 7), true);
			board[7, 1] = _pieceFactory.CreatePiece(PieceType.Knight, new Position(1, 7), true);
			board[7, 2] = _pieceFactory.CreatePiece(PieceType.Bishop, new Position(2, 7), true);
			board[7, 3] = _pieceFactory.CreatePiece(PieceType.Queen, new Position(3, 7), true);
			board[7, 4] = _pieceFactory.CreatePiece(PieceType.King, new Position(4, 7), true);
			board[7, 5] = _pieceFactory.CreatePiece(PieceType.Bishop, new Position(5, 7), true);
			board[7, 6] = _pieceFactory.CreatePiece(PieceType.Knight, new Position(6, 7), true);
			board[7, 7] = _pieceFactory.CreatePiece(PieceType.Rook, new Position(7, 7), true);

			whiteKingPos = new Position(4, 7);
			blackKingPos = new Position(4, 0);

			return board;
		}
	}

}
