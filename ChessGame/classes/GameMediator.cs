using ChessClassLibrary;
using ChessGame.Classes.Pieces;
using ChessGame.interfaces;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChessGame.Classes
{
	public class GameMediator : IGameMediator
	{
		private readonly IGameControler _gameControler;
		private readonly IBoardPanel _boardPanel;
		private readonly INetworkClient _networkClient;
		private bool _isExiting = false;
		private Position _selectedPiece;
		private readonly Action _closeFormAction;

		/// <summary>
		/// Initializes a new instance of the <see cref="GameMediator"/> class.
		/// </summary>
		/// <param name="gameControler">The game controller managing the chess game logic.</param>
		/// <param name="boardPanel">The board panel responsible for rendering the chess board.</param>
		/// <param name="networkClient">The network client handling communication with the opponent.</param>
		/// <param name="closeFormAction">The action to invoke when closing the game form.</param>
		/// <exception cref="ArgumentNullException">Thrown when any of the parameters are null.</exception>
		public GameMediator(IGameControler gameControler, IBoardPanel boardPanel, INetworkClient networkClient, Action closeFormAction)
		{
			_gameControler = gameControler ?? throw new ArgumentNullException(nameof(gameControler));
			_boardPanel = boardPanel ?? throw new ArgumentNullException(nameof(boardPanel));
			_networkClient = networkClient ?? throw new ArgumentNullException(nameof(networkClient));
			_closeFormAction = closeFormAction ?? throw new ArgumentNullException(nameof(closeFormAction));

			_boardPanel.SetMediator(this);
			_networkClient.MoveReceived += HandleNetworkMove;
			_networkClient.DisconnectAction += HandleOpponentDisconnect;
			_gameControler.OnSideChanged += () => _boardPanel.Invalidate();
		}

		/// <summary>
		/// Handles the opponent's disconnection by displaying a message and closing the form.
		/// </summary>
		/// <param name="name">The name of the opponent who disconnected.</param>
		private void HandleOpponentDisconnect(string name)
		{
			if (_isExiting) return;

			MessageBox.Show($"Суперник {name} вийшов із гри.", "Інформація", MessageBoxButtons.OK, MessageBoxIcon.Information);
			_closeFormAction?.Invoke();
		}

		/// <summary>
		/// Processes a local move attempt by validating and executing it if valid.
		/// </summary>
		/// <param name="start">The starting position of the piece to move.</param>
		/// <param name="end">The target position for the move.</param>
		public void HandleLocalMove(Position start, Position end)
		{
			Piece piece = _gameControler.Board[start.Y, start.X];
			if (piece == null)
			{
				Console.WriteLine($"No piece at {start}");
				return;
			}

			if (IsValidMove(piece, start, end))
			{
				Console.WriteLine($"Executing move from {start} to {end}");
				ExecuteMove(piece, start, end);
				_networkClient.SendMove(start, end);
				_boardPanel.Invalidate();
			}
			else
			{
				Console.WriteLine($"Invalid move from {start} to {end}");
			}
			_selectedPiece = null;
		}

		/// <summary>
		/// Attempts to select a piece at the specified position if it belongs to the current player.
		/// </summary>
		/// <param name="position">The position of the piece to select.</param>
		public void TrySelectPiece(Position position)
		{
			Piece piece = _gameControler.Board[position.Y, position.X];
			if (piece != null && piece.IsWhite == _gameControler.IsWhiteTurn)
			{
				_selectedPiece = position;
				Console.WriteLine($"Selected piece at {position}");
				_boardPanel.Invalidate();
			}
			else
			{
				Console.WriteLine($"Cannot select piece at {position}");
			}
		}

		/// <summary>
		/// Determines if the current player can process a click based on the game state.
		/// </summary>
		/// <param name="isWhiteTurn">True if it is the white player's turn; otherwise, false.</param>
		/// <returns>True if the player can process a click; otherwise, false.</returns>
		public bool CanProcessClick(bool isWhiteTurn)
		{
			return _networkClient.IsLocalPlayerWhite == isWhiteTurn && !_gameControler.GameEnded;
		}

		/// <summary>
		/// Validates whether a move from the start to the end position is legal.
		/// </summary>
		/// <param name="start">The starting position of the piece.</param>
		/// <param name="end">The target position for the move.</param>
		/// <returns>True if the move is valid; otherwise, false.</returns>
		public bool IsValidMoveTarget(Position start, Position end)
		{
			Piece piece = _gameControler.Board[start.Y, start.X];
			if (piece == null)
			{
				Console.WriteLine($"No piece at {start} for move to {end}");
				return false;
			}

			bool isValidMove = piece.IsValidMove(end, _gameControler.Board) &&
							  (_gameControler.Board[end.Y, end.X] == null ||
							   _gameControler.Board[end.Y, end.X].IsWhite != piece.IsWhite);

			if (!isValidMove)
			{
				Console.WriteLine($"Move from {start} to {end} is not valid");
				return false;
			}

			bool isKingMove = piece is King;
			bool isKingInCheck = _gameControler.IsKingInCheck(piece.IsWhite);

			bool result = isKingMove
				? !_gameControler.IsSquareUnderAttack(end, !piece.IsWhite)
				: !isKingInCheck || _gameControler.IsMoveResolvingCheck(start, end, piece.IsWhite);

			Console.WriteLine($"Move from {start} to {end} is {(result ? "valid" : "invalid")}");
			return result;
		}

		/// <summary>
		/// Disconnects the network client and sends a leave message to the opponent.
		/// </summary>
		public void Disconnect()
		{
			_isExiting = true;
			_networkClient.SendLeave();
			_networkClient.Disconnect();
		}

		/// <summary>
		/// Applies board rotation for black players by transforming the graphics context.
		/// </summary>
		/// <param name="g">The graphics context used for rendering the board.</param>
		/// <param name="width">The width of the board panel in pixels.</param>
		/// <param name="height">The height of the board panel in pixels.</param>
		public void ApplyBoardRotation(Graphics g, int width, int height)
		{
			if (!_networkClient.IsLocalPlayerWhite)
			{
				g.TranslateTransform(width, height);
				g.RotateTransform(180);
			}
		}

		/// <summary>
		/// Checks if the specified position is part of the last move made.
		/// </summary>
		/// <param name="position">The position to check.</param>
		/// <returns>True if the position is the start or end of the last move; otherwise, false.</returns>
		public bool IsLastMovePosition(Position position)
		{
			if (!_gameControler.LastMove.HasValue) return false;
			var (fromPos, toPos) = _gameControler.LastMove.Value;
			return (position.Y == fromPos.Y && position.X == fromPos.X) ||
				   (position.Y == toPos.Y && position.X == toPos.X);
		}

		/// <summary>
		/// Checks if the specified position contains the currently selected piece.
		/// </summary>
		/// <param name="position">The position to check.</param>
		/// <returns>True if the position matches the selected piece; otherwise, false.</returns>
		public bool IsSelectedPiece(Position position)
		{
			return _selectedPiece != null && _selectedPiece.Y == position.Y && _selectedPiece.X == position.X;
		}

		/// <summary>
		/// Determines if piece icons should be rotated based on the player's perspective.
		/// </summary>
		/// <returns>True if the local player is black and icons should be rotated; otherwise, false.</returns>
		public bool ShouldRotateIcons()
		{
			return !_networkClient.IsLocalPlayerWhite;
		}

		/// <summary>
		/// Retrieves the current state of the chess board.
		/// </summary>
		/// <returns>A 2D array representing the chess board with pieces.</returns>
		public Piece[,] GetBoard()
		{
			return _gameControler.Board;
		}

		/// <summary>
		/// Gets the current player's turn.
		/// </summary>
		/// <returns>True if it is the white player's turn; otherwise, false.</returns>
		public bool GetIsWhiteTurn()
		{
			return _gameControler.IsWhiteTurn;
		}

		/// <summary>
		/// Checks if the king of the specified color is in check.
		/// </summary>
		/// <param name="forWhite">True to check the white king; false for the black king.</param>
		/// <returns>True if the king is in check; otherwise, false.</returns>
		public bool IsKingInCheck(bool forWhite)
		{
			return _gameControler.IsKingInCheck(forWhite);
		}

		/// <summary>
		/// Gets the position of the currently selected piece.
		/// </summary>
		/// <returns>The position of the selected piece, or null if no piece is selected.</returns>
		public Position GetSelectedPiece()
		{
			return _selectedPiece;
		}

		/// <summary>
		/// Validates a move for a specific piece from start to end position.
		/// </summary>
		/// <param name="piece">The piece to move.</param>
		/// <param name="start">The starting position of the piece.</param>
		/// <param name="end">The target position for the move.</param>
		/// <returns>True if the move is valid; otherwise, false.</returns>
		private bool IsValidMove(Piece piece, Position start, Position end)
		{
			return IsValidMoveTarget(start, end);
		}

		/// <summary>
		/// Executes a move on the board, updating the game state and checking for check.
		/// </summary>
		/// <param name="piece">The piece to move.</param>
		/// <param name="start">The starting position of the piece.</param>
		/// <param name="end">The target position for the move.</param>
		private void ExecuteMove(Piece piece, Position start, Position end)
		{
			Piece capturedPiece = _gameControler.Board[end.Y, end.X];
			_gameControler.Board[end.Y, end.X] = piece;
			_gameControler.Board[start.Y, start.X] = null;
			piece.UpdatePosition(end);

			if (piece is King)
			{
				_gameControler.UpdateKingPosition(end, piece.IsWhite);
			}
			else
			{
				Position kingPos = _gameControler.GetKingPosition(piece.IsWhite);
				if (_gameControler.IsSquareUnderAttack(kingPos, !piece.IsWhite))
				{
					_gameControler.Board[start.Y, start.X] = piece;
					_gameControler.Board[end.Y, end.X] = capturedPiece;
					piece.UpdatePosition(start);
					return;
				}
			}

			_gameControler.LastMove = (start, end);
			_gameControler.IsWhiteTurn = !_gameControler.IsWhiteTurn;
			_gameControler.CheckGameState();
		}

		/// <summary>
		/// Handles a move received from the network, updating the board and redrawing it.
		/// </summary>
		/// <param name="start">The starting position of the opponent's move.</param>
		/// <param name="end">The target position of the opponent's move.</param>
		private void HandleNetworkMove(Position start, Position end)
		{
			_gameControler.MovePieceFromNetwork(start, end);
			_boardPanel.Invalidate();
		}
	}
}