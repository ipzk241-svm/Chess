using ChessGame.Classes.Pieces;
using System;
using System.Windows.Forms;

namespace ChessGame.Classes
{
	public class GameControler
	{
		private static GameControler _instance;
		public Piece[,] Board;
		private bool _isWhiteTurn;
		public bool IsWhiteTurn
		{
			get
			{
				return _isWhiteTurn;
			}
			set
			{
				_isWhiteTurn = value;
				OnSideChanged?.Invoke();
			}
		}
		public (Position from, Position to)? LastMove;
		public Position WhiteKingPos;
		public Position BlackKingPos;
		public bool GameEnded;
		private const int Size = 8;
		public Action OnSideChanged;
		private GameControler()
		{
			Board = new Piece[Size, Size];
			IsWhiteTurn = true;
			LastMove = null;
			GameEnded = false;
			InitializeBoard();
		}

		public static GameControler Instance
		{
			get
			{
				if (_instance == null)
					_instance = new GameControler();
				return _instance;
			}
		}

		public void InitializeBoard()
		{
			var pieceFactory = new PieceFactory();

			for (int i = 0; i < 8; i++)
			{
				Board[1, i] = pieceFactory.CreatePiece(PieceType.Pawn, new Position(i, 1), false);
				Board[6, i] = pieceFactory.CreatePiece(PieceType.Pawn, new Position(i, 6), true);
			}

			PieceType[] layout =
			[
		PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Queen,
		PieceType.King, PieceType.Bishop, PieceType.Knight, PieceType.Rook
			];

			for (int i = 0; i < 8; i++)
			{
				Board[0, i] = pieceFactory.CreatePiece(layout[i], new Position(i, 0), false);
				Board[7, i] = pieceFactory.CreatePiece(layout[i], new Position(i, 7), true);
			}

			BlackKingPos = new Position(4, 0);
			WhiteKingPos = new Position(4, 7);
		}


		public void StartGame()
		{
			Board = new Piece[Size, Size];
			InitializeBoard();
			IsWhiteTurn = true;
			LastMove = null;
			GameEnded = false;
		}

		public void UpdateKingPosition(Position pos, bool isWhite)
		{
			if (isWhite)
				WhiteKingPos.UpdatePosition(pos.X, pos.Y);
			else
				BlackKingPos.UpdatePosition(pos.X, pos.Y);
		}

		public Position GetKingPosition(bool isWhite)
		{
			return isWhite ? WhiteKingPos : BlackKingPos;
		}

		public bool IsSquareUnderAttack(Position targetPos, bool attackerIsWhite)
		{
			for (int row = 0; row < Size; row++)
			{
				for (int col = 0; col < Size; col++)
				{
					Piece piece = Board[row, col];
					if (piece != null && piece.IsWhite == attackerIsWhite)
					{
						if (piece.IsValidMove(targetPos, Board, isCheckEvaluation: true))
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		public bool IsKingInCheck(bool forWhite)
		{
			Position kingPos = GetKingPosition(forWhite);
			return IsSquareUnderAttack(kingPos, !forWhite);
		}

		public bool IsMoveResolvingCheck(Position startPos, Position endPos, bool forWhite)
		{
			Position kingPos = GetKingPosition(forWhite);
			Piece piece = Board[startPos.Y, startPos.X];
			Piece capturedPiece = Board[endPos.Y, endPos.X];
			Board[endPos.Y, endPos.X] = piece;
			Board[startPos.Y, startPos.X] = null;

			if (piece is King)
			{
				kingPos = endPos;
			}

			bool stillInCheck = IsSquareUnderAttack(kingPos, !forWhite);

			Board[startPos.Y, startPos.X] = piece;
			Board[endPos.Y, endPos.X] = capturedPiece;

			return !stillInCheck;
		}

		public bool HasLegalMoves(bool forWhite)
		{
			for (int startRow = 0; startRow < Size; startRow++)
			{
				for (int startCol = 0; startCol < Size; startCol++)
				{
					Piece piece = Board[startRow, startCol];
					if (piece != null && piece.IsWhite == forWhite)
					{
						for (int targetRow = 0; targetRow < Size; targetRow++)
						{
							for (int targetCol = 0; targetCol < Size; targetCol++)
							{
								Position targetPos = new Position(targetCol, targetRow);
								if (piece.IsValidMove(targetPos, Board) &&
									(Board[targetRow, targetCol] == null || Board[targetRow, targetCol].IsWhite != piece.IsWhite))
								{
									Piece tempPiece = Board[targetRow, targetCol];
									Board[targetRow, targetCol] = piece;
									Board[startRow, startCol] = null;

									Position originalKingPos = GetKingPosition(forWhite);
									Position kingPosToCheck = piece is King ? targetPos : originalKingPos;

									bool wouldBeInCheck = IsSquareUnderAttack(kingPosToCheck, !forWhite);

									Board[startRow, startCol] = piece;
									Board[targetRow, targetCol] = tempPiece;

									if (!wouldBeInCheck)
									{
										return true;
									}
								}
							}
						}
					}
				}
			}
			return false;
		}
		public void MovePieceFromNetwork(Position start, Position end)
		{
			Piece piece = Board[start.Y, start.X];
			Piece captured = Board[end.Y, end.X];

			Board[end.Y, end.X] = piece;
			Board[start.Y, start.X] = null;
			piece.UpdatePosition(end);

			if (piece is King)
				UpdateKingPosition(end, piece.IsWhite);

			LastMove = (start, end);
			IsWhiteTurn = !IsWhiteTurn;
			CheckGameState();
		}

		public void CheckGameState()
		{
			Position kingPos = GetKingPosition(IsWhiteTurn);
			bool isInCheck = IsSquareUnderAttack(kingPos, !IsWhiteTurn);
			bool hasLegalMoves = HasLegalMoves(IsWhiteTurn);

			if (isInCheck)
			{
				if (!hasLegalMoves)
				{
					if (IsWhiteTurn)
					{
						EndGame("Checkmate! Black wins.");
					}
					else
					{
						EndGame("Checkmate! White wins.");
					}
				}
			}
			else if (!hasLegalMoves)
			{
				EndGame("Stalemate! The game is a draw.");
			}
		}

		private void EndGame(string message)
		{
			GameEnded = true;
			MessageBox.Show(message, "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Information);

		}

	}
}