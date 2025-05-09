using ChessClassLibrary;
using ChessGame.Classes.Pieces;
using ChessGame.interfaces;
using System;
using System.Windows.Forms;

namespace ChessGame.Classes
{
	public class GameControler : IGameControler
	{
		private const int BoardSize = 8;
		private static GameControler _instance;
		private readonly Piece[,] _board;
		private bool _isWhiteTurn;
		private Position _whiteKingPos;
		private Position _blackKingPos;

		/// <summary>
		/// Singleton instance of the GameControler.
		/// </summary>
		public static GameControler Instance => _instance ??= new GameControler();

		public Piece[,] Board => _board;

		/// <summary>
		/// Gets or sets the current turn. True for white's turn, false for black's turn.
		/// </summary>
		public bool IsWhiteTurn
		{
			get => _isWhiteTurn;
			set
			{
				_isWhiteTurn = value;
				OnSideChanged?.Invoke();
			}
		}

		/// <summary>
		/// Represents the last move made, if any.
		/// </summary>
		public (Position from, Position to)? LastMove { get; set; }

		/// <summary>
		/// Indicates whether the game has ended.
		/// </summary>
		public bool GameEnded { get; private set; }

		/// <summary>
		/// Event triggered when the turn changes.
		/// </summary>
		public event Action OnSideChanged;

		private GameControler()
		{
			_board = new Piece[BoardSize, BoardSize];
			_isWhiteTurn = true;
			LastMove = null;
			GameEnded = false;
			InitializeBoard();
		}

		/// <summary>
		/// Initializes the chessboard with pieces in their starting positions.
		/// </summary>
		private void InitializeBoard()
		{
			var pieceFactory = new PieceFactory();

			for (int i = 0; i < BoardSize; i++)
			{
				_board[1, i] = pieceFactory.CreatePiece(PieceType.Pawn, new Position(i, 1), false);
				_board[6, i] = pieceFactory.CreatePiece(PieceType.Pawn, new Position(i, 6), true);
			}

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

		/// <summary>
		/// Starts a new game by resetting the board and game state.
		/// </summary>
		public void StartGame()
		{
			Array.Clear(_board, 0, _board.Length);
			InitializeBoard();
			IsWhiteTurn = true;
			LastMove = null;
			GameEnded = false;
		}

		/// <summary>
		/// Updates the position of the king.
		/// </summary>
		/// <param name="pos">The new position of the king.</param>
		/// <param name="isWhite">Indicates whether the king is white or black.</param>
		public void UpdateKingPosition(Position pos, bool isWhite)
		{
			(isWhite ? ref _whiteKingPos : ref _blackKingPos) = pos;
		}

		/// <summary>
		/// Gets the current position of the king.
		/// </summary>
		/// <param name="isWhite">Indicates whether to get the white king's position or black king's position.</param>
		/// <returns>The position of the king.</returns>
		public Position GetKingPosition(bool isWhite) => isWhite ? _whiteKingPos : _blackKingPos;

		/// <summary>
		/// Checks if a square is under attack by any opponent piece.
		/// </summary>
		/// <param name="targetPos">The target position to check.</param>
		/// <param name="attackerIsWhite">Indicates whether the attacker is white.</param>
		/// <returns>True if the square is under attack, false otherwise.</returns>
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

		/// <summary>
		/// Checks if the specified king is in check.
		/// </summary>
		/// <param name="forWhite">Indicates whether to check the white or black king.</param>
		/// <returns>True if the king is in check, false otherwise.</returns>
		public bool IsKingInCheck(bool forWhite)
		{
			Position kingPos = GetKingPosition(forWhite);
			return IsSquareUnderAttack(kingPos, !forWhite);
		}

		/// <summary>
		/// Checks if a move resolves a check.
		/// </summary>
		/// <param name="startPos">The starting position of the piece.</param>
		/// <param name="endPos">The target position of the move.</param>
		/// <param name="forWhite">Indicates whether to check for white or black king's check.</param>
		/// <returns>True if the move resolves the check, false otherwise.</returns>
		public bool IsMoveResolvingCheck(Position startPos, Position endPos, bool forWhite)
		{
			Piece piece = _board[startPos.Y, startPos.X];
			Piece capturedPiece = _board[endPos.Y, endPos.X];

			_board[endPos.Y, endPos.X] = piece;
			_board[startPos.Y, startPos.X] = null;

			Position kingPos = piece is King ? endPos : GetKingPosition(forWhite);
			bool stillInCheck = IsSquareUnderAttack(kingPos, !forWhite);

			_board[startPos.Y, startPos.X] = piece;
			_board[endPos.Y, endPos.X] = capturedPiece;

			return !stillInCheck;
		}

		/// <summary>
		/// Checks if there are any legal moves for the specified color.
		/// </summary>
		/// <param name="forWhite">Indicates whether to check for white or black pieces.</param>
		/// <returns>True if there are legal moves, false otherwise.</returns>
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

		/// <summary>
		/// Checks if a piece has any valid moves.
		/// </summary>
		/// <param name="piece">The piece to check.</param>
		/// <param name="startPos">The starting position of the piece.</param>
		/// <param name="forWhite">Indicates whether to check for white or black pieces.</param>
		/// <returns>True if the piece has valid moves, false otherwise.</returns>
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

		/// <summary>
		/// Checks if a move is valid for a piece.
		/// </summary>
		/// <param name="piece">The piece to move.</param>
		/// <param name="startPos">The starting position of the piece.</param>
		/// <param name="targetPos">The target position of the move.</param>
		/// <returns>True if the move is valid, false otherwise.</returns>
		private bool IsValidMove(Piece piece, Position startPos, Position targetPos)
		{
			return piece.IsValidMove(targetPos, _board) &&
				   (_board[targetPos.Y, targetPos.X] == null ||
					_board[targetPos.Y, targetPos.X].IsWhite != piece.IsWhite);
		}

		/// <summary>
		/// Checks if a move would avoid putting the king in check.
		/// </summary>
		/// <param name="piece">The piece to move.</param>
		/// <param name="startPos">The starting position of the piece.</param>
		/// <param name="targetPos">The target position of the move.</param>
		/// <param name="forWhite">Indicates whether to check for white or black king's check.</param>
		/// <returns>True if the move would avoid putting the king in check, false otherwise.</returns>
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

		/// <summary>
		/// Moves a piece from one position to another over the network.
		/// </summary>
		/// <param name="start">The starting position of the piece.</param>
		/// <param name="end">The target position of the move.</param>
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

		/// <summary>
		/// Checks the current game state (check, checkmate, stalemate).
		/// </summary>
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

		/// <summary>
		/// Ends the game and displays the result message.
		/// </summary>
		/// <param name="message">The game result message.</param>
		private void EndGame(string message)
		{
			GameEnded = true;
			MessageBox.Show(message, "Game Over", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
