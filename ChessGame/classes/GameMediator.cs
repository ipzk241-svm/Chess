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

		private void HandleOpponentDisconnect(string name)
		{
			if (_isExiting) return;

			MessageBox.Show($"Суперник {name} вийшов із гри.", "Інформація", MessageBoxButtons.OK, MessageBoxIcon.Information);
			_closeFormAction?.Invoke();
		}

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

		public bool CanProcessClick(bool isWhiteTurn)
		{
			return _networkClient.IsLocalPlayerWhite == isWhiteTurn && !_gameControler.GameEnded;
		}

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

		public void Disconnect()
		{
			_isExiting = true;
			_networkClient.SendLeave();
			_networkClient.Disconnect();
		}

		public void ApplyBoardRotation(Graphics g, int width, int height)
		{
			if (!_networkClient.IsLocalPlayerWhite)
			{
				g.TranslateTransform(width, height);
				g.RotateTransform(180);
			}
		}

		public bool IsLastMovePosition(Position position)
		{
			if (!_gameControler.LastMove.HasValue) return false;
			var (fromPos, toPos) = _gameControler.LastMove.Value;
			return (position.Y == fromPos.Y && position.X == fromPos.X) ||
				   (position.Y == toPos.Y && position.X == toPos.X);
		}

		public bool IsSelectedPiece(Position position)
		{
			return _selectedPiece != null && _selectedPiece.Y == position.Y && _selectedPiece.X == position.X;
		}

		public bool ShouldRotateIcons()
		{
			return !_networkClient.IsLocalPlayerWhite;
		}

		public Piece[,] GetBoard()
		{
			return _gameControler.Board;
		}

		public bool GetIsWhiteTurn()
		{
			return _gameControler.IsWhiteTurn;
		}

		public bool IsKingInCheck(bool forWhite)
		{
			return _gameControler.IsKingInCheck(forWhite);
		}

		public Position GetSelectedPiece()
		{
			return _selectedPiece;
		}

		private bool IsValidMove(Piece piece, Position start, Position end)
		{
			return IsValidMoveTarget(start, end);
		}

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

		private void HandleNetworkMove(Position start, Position end)
		{
			_gameControler.MovePieceFromNetwork(start, end);
			_boardPanel.Invalidate();
		}

	}
}