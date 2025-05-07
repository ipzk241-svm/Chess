using ChessGame.Classes.Pieces;
using System;
using System.Windows.Forms;

namespace ChessGame.Classes
{
	public class GameControler
	{
		private const int BoardSize = 8;
		private static GameControler _instance;
		private readonly Piece[,] _board;
		private bool _isWhiteTurn;
		private Position _whiteKingPos;
		private Position _blackKingPos;

		public static GameControler Instance => _instance ??= new GameControler();

		public Piece[,] Board => _board;
		public bool IsWhiteTurn
		{
			get => _isWhiteTurn;
			set
			{
				_isWhiteTurn = value;
				OnSideChanged?.Invoke();
			}
		}
		public (Position from, Position to)? LastMove { get; set; }
		public bool GameEnded { get; private set; }
		public Action OnSideChanged { get; set; }

		private GameControler()
		{
			_board = new Piece[BoardSize, BoardSize];
			_isWhiteTurn = true;
			LastMove = null;
			GameEnded = false;
			InitializeBoard();
		}

		private void InitializeBoard()
		{
			var pieceFactory = new PieceFactory();

			// Initialize pawns
			for (int i = 0; i < BoardSize; i++)
			{
				_board[1, i] = pieceFactory.CreatePiece(PieceType.Pawn, new Position(i, 1), false);
				_board[6, i] = pieceFactory.CreatePiece(PieceType.Pawn, new Position(i, 6), true);
			}

			// Initialize other pieces
			PieceType[] layout = { PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Queen,
								   PieceType.King, PieceType.Bishop, PieceType.Knight, PieceType.Rook };

			for (int i = 0; i < BoardSize; i++)
			{
				_board[0, i] = pieceFactory.CreatePiece(layout[i], new Position(i, 0), false);
				_board[7, i] = pieceFactory.CreatePiece(layout[i], new Position(i, 7), true);
			}

			_blackKingPos = new Position(4, 0);
			_whiteKingPos = new Position(4, 7);
		}

		public void StartGame()
		{
			Array.Clear(_board, 0, _board.Length);
			InitializeBoard();
			IsWhiteTurn = true;
			LastMove = null;
			GameEnded = false;
		}

		public void UpdateKingPosition(Position pos, bool isWhite)
		{
			(isWhite ? ref _whiteKingPos : ref _blackKingPos) = pos;
		}

		public Position GetKingPosition(bool isWhite) => isWhite ? _whiteKingPos : _blackKingPos;

		public bool IsSquareUnderAttack(Position targetPos, bool attackerIsWhite)
		{
			for (int row = 0; row < BoardSize; row++)
			{
				for (int col = 0; col < BoardSize; col++)
				{
					Piece piece = _board[row, col];
					if (piece != null && piece.IsWhite == attackerIsWhite &&
						piece.IsValidMove(targetPos, _board, isCheckEvaluation: true))
					{
						return true;
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
			Piece piece = _board[startPos.Y, startPos.X];
			Piece capturedPiece = _board[endPos.Y, endPos.X];

			// Simulate move
			_board[endPos.Y, endPos.X] = piece;
			_board[startPos.Y, startPos.X] = null;

			Position kingPos = piece is King ? endPos : GetKingPosition(forWhite);
			bool stillInCheck = IsSquareUnderAttack(kingPos, !forWhite);

			// Revert move
			_board[startPos.Y, startPos.X] = piece;
			_board[endPos.Y, endPos.X] = capturedPiece;

			return !stillInCheck;
		}

		public bool HasLegalMoves(bool forWhite)
		{
			for (int startRow = 0; startRow < BoardSize; startRow++)
			{
				for (int startCol = 0; startCol < BoardSize; startCol++)
				{
					Piece piece = _board[startRow, startCol];
					if (piece != null && piece.IsWhite == forWhite &&
						HasValidMoveForPiece(piece, new Position(startCol, startRow), forWhite))
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool HasValidMoveForPiece(Piece piece, Position startPos, bool forWhite)
		{
			for (int targetRow = 0; targetRow < BoardSize; targetRow++)
			{
				for (int targetCol = 0; targetCol < BoardSize; targetCol++)
				{
					Position targetPos = new(targetCol, targetRow);
					if (IsValidMove(piece, startPos, targetPos) &&
						WouldMoveAvoidCheck(piece, startPos, targetPos, forWhite))
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool IsValidMove(Piece piece, Position startPos, Position targetPos)
		{
			return piece.IsValidMove(targetPos, _board) &&
				   (_board[targetPos.Y, targetPos.X] == null ||
					_board[targetPos.Y, targetPos.X].IsWhite != piece.IsWhite);
		}

		private bool WouldMoveAvoidCheck(Piece piece, Position startPos, Position targetPos, bool forWhite)
		{
			Piece capturedPiece = _board[targetPos.Y, targetPos.X];
			_board[targetPos.Y, targetPos.X] = piece;
			_board[startPos.Y, startPos.X] = null;

			Position kingPos = piece is King ? targetPos : GetKingPosition(forWhite);
			bool wouldBeInCheck = IsSquareUnderAttack(kingPos, !forWhite);

			_board[startPos.Y, startPos.X] = piece;
			_board[targetPos.Y, targetPos.X] = capturedPiece;

			return !wouldBeInCheck;
		}

		public void MovePieceFromNetwork(Position start, Position end)
		{
			Piece piece = _board[start.Y, start.X];
			_board[end.Y, end.X] = piece;
			_board[start.Y, start.X] = null;
			piece.UpdatePosition(end);

			if (piece is King)
				UpdateKingPosition(end, piece.IsWhite);

			LastMove = (start, end);
			IsWhiteTurn = !IsWhiteTurn;
			CheckGameState();
		}

		public void CheckGameState()
		{
			bool isInCheck = IsKingInCheck(IsWhiteTurn);
			bool hasLegalMoves = HasLegalMoves(IsWhiteTurn);

			if (isInCheck && !hasLegalMoves)
			{
				EndGame(IsWhiteTurn ? "Checkmate! Black wins." : "Checkmate! White wins.");
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