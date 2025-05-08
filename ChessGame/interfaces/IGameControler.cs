using ChessGame.Classes.Pieces;
using ChessGame.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessGame.interfaces
{
	public interface IGameControler
	{
		Piece[,] Board { get; }
		bool IsWhiteTurn { get; set; }
		(Position from, Position to)? LastMove { get; set; }
		bool GameEnded { get; }
		event Action OnSideChanged;

		void StartGame();
		void UpdateKingPosition(Position pos, bool isWhite);
		Position GetKingPosition(bool isWhite);
		bool IsSquareUnderAttack(Position targetPos, bool attackerIsWhite);
		bool IsKingInCheck(bool forWhite);
		bool IsMoveResolvingCheck(Position startPos, Position endPos, bool forWhite);
		void MovePieceFromNetwork(Position start, Position end);
		void CheckGameState();
	}
}
