using ChessClassLibrary;
using ChessGame.Classes;
using ChessGame.Classes.Pieces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessGame.interfaces
{
	public interface IGameMediator
	{
		void HandleLocalMove(Position start, Position end);
		void TrySelectPiece(Position position);
		void Disconnect();
		bool CanProcessClick(bool isWhiteTurn);
		bool IsValidMoveTarget(Position start, Position end);
		void ApplyBoardRotation(Graphics g, int width, int height);
		bool IsLastMovePosition(Position position);
		bool IsSelectedPiece(Position position);
		bool ShouldRotateIcons();
		Piece[,] GetBoard();
		bool GetIsWhiteTurn();
		bool IsKingInCheck(bool forWhite);
		Position GetSelectedPiece();
	}
}
