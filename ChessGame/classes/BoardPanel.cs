using ChessGame.Classes.Pieces;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChessGame.Classes
{
	public class BoardPanel : Panel
	{
		private const int BoardSize = 8;
		private readonly int _cellSize;
		private Position _selectedPiece = null;
		private NetworkClient _networkClient;

		public BoardPanel(int cellSize = 60)
		{
			_cellSize = cellSize;
			this.Width = cellSize * BoardSize;
			this.Height = cellSize * BoardSize;
			this.DoubleBuffered = true;
			this.Paint += Board_Paint;
			this.MouseClick += BoardPanel_MouseClick;
		}

		public void SetNetworkClient(NetworkClient client) => _networkClient = client;

		private void Board_Paint(object sender, PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			ApplyBoardRotation(g);
			var board = GameControler.Instance.Board;

			for (int row = 0; row < BoardSize; row++)
			{
				for (int col = 0; col < BoardSize; col++)
				{
					DrawCellBackground(g, row, col, board);
					DrawPieceIcon(g, row, col, board);
					g.DrawRectangle(Pens.Black, col * _cellSize, row * _cellSize, _cellSize, _cellSize);
				}
			}
		}

		private void ApplyBoardRotation(Graphics g)
		{
			if (_networkClient != null && !_networkClient.IsLocalPlayerWhite)
			{
				g.TranslateTransform(Width, Height);
				g.RotateTransform(180);
			}
		}

		private void DrawCellBackground(Graphics g, int row, int col, Piece[,] board)
		{
			Brush backgroundBrush = (row + col) % 2 == 0 ? Brushes.White : Brushes.Gray;
			Rectangle cellRect = new Rectangle(col * _cellSize, row * _cellSize, _cellSize, _cellSize);

			if (IsLastMovePosition(row, col))
				backgroundBrush = new SolidBrush(Color.YellowGreen);
			else if (IsKingInCheck(row, col, board))
				backgroundBrush = new SolidBrush(Color.MediumVioletRed);
			else if (IsSelectedPiece(row, col))
				backgroundBrush = new SolidBrush(Color.Yellow);
			else if (_selectedPiece != null && IsValidMoveTarget(row, col, board))
				backgroundBrush = GetMoveHighlightBrush(row, col, board);

			g.FillRectangle(backgroundBrush, cellRect);
		}

		private bool IsLastMovePosition(int row, int col)
		{
			if (!GameControler.Instance.LastMove.HasValue) return false;
			var (fromPos, toPos) = GameControler.Instance.LastMove.Value;
			return (row == fromPos.Y && col == fromPos.X) || (row == toPos.Y && col == toPos.X);
		}

		private bool IsKingInCheck(int row, int col, Piece[,] board)
		{
			return board[row, col] is King &&
				   board[row, col].IsWhite == GameControler.Instance.IsWhiteTurn &&
				   GameControler.Instance.IsKingInCheck(GameControler.Instance.IsWhiteTurn);
		}

		private bool IsSelectedPiece(int row, int col)
		{
			return _selectedPiece != null && _selectedPiece.Y == row && _selectedPiece.X == col;
		}

		private bool IsValidMoveTarget(int row, int col, Piece[,] board)
		{
			Position endPos = new(col, row);
			Piece piece = board[_selectedPiece.Y, _selectedPiece.X];
			bool isValidMove = piece != null && piece.IsValidMove(endPos, board) &&
							  (board[row, col] == null || board[row, col].IsWhite != piece.IsWhite);

			if (!isValidMove) return false;

			bool isKingMove = piece is King;
			bool isKingInCheck = GameControler.Instance.IsKingInCheck(piece.IsWhite);

			return isKingMove
				? !GameControler.Instance.IsSquareUnderAttack(endPos, !piece.IsWhite)
				: !isKingInCheck || GameControler.Instance.IsMoveResolvingCheck(_selectedPiece, endPos, piece.IsWhite);
		}

		private Brush GetMoveHighlightBrush(int row, int col, Piece[,] board)
		{
			Piece piece = board[_selectedPiece.Y, _selectedPiece.X];
			return piece is King ? new SolidBrush(Color.Green) :
				   board[row, col] != null ? new SolidBrush(Color.Red) :
				   new SolidBrush(Color.LightGreen);
		}

		private void DrawPieceIcon(Graphics g, int row, int col, Piece[,] board)
		{
			if (board[row, col] == null) return;

			Image icon = board[row, col].Icon;
			if (!_networkClient.IsLocalPlayerWhite)
			{
				icon = (Image)icon.Clone();
				icon.RotateFlip(RotateFlipType.Rotate180FlipNone);
			}
			g.DrawImage(icon, col * _cellSize + 5, row * _cellSize + 5, 50, 50);
		}

		private void BoardPanel_MouseClick(object sender, MouseEventArgs e)
		{
			if (!CanProcessClick()) return;

			var (clickedRow, clickedCol) = CalculateClickPosition(e);
			if (!IsValidPosition(clickedRow, clickedCol)) return;

			var board = GameControler.Instance.Board;

			if (_selectedPiece == null)
			{
				TrySelectPiece(clickedRow, clickedCol, board);
			}
			else
			{
				TryMovePiece(clickedRow, clickedCol, board);
			}
			Invalidate();
		}

		private bool CanProcessClick()
		{
			return _networkClient.IsLocalPlayerWhite == GameControler.Instance.IsWhiteTurn &&
				   !GameControler.Instance.GameEnded;
		}

		private (int row, int col) CalculateClickPosition(MouseEventArgs e)
		{
			return _networkClient.IsLocalPlayerWhite
				? (e.Y / _cellSize, e.X / _cellSize)
				: ((Height - e.Y) / _cellSize, (Width - e.X) / _cellSize);
		}

		private bool IsValidPosition(int row, int col)
		{
			return row >= 0 && row < BoardSize && col >= 0 && col < BoardSize;
		}

		private void TrySelectPiece(int row, int col, Piece[,] board)
		{
			if (board[row, col] != null && board[row, col].IsWhite == GameControler.Instance.IsWhiteTurn)
			{
				_selectedPiece = new Position(col, row);
			}
		}

		private void TryMovePiece(int clickedRow, int clickedCol, Piece[,] board)
		{
			Position endPos = new(clickedCol, clickedRow);
			Piece piece = board[_selectedPiece.Y, _selectedPiece.X];

			if (IsValidPosition(clickedRow, clickedCol) && IsValidMoveTarget(clickedRow, clickedCol, board))
			{
				ExecuteMove(piece, _selectedPiece, endPos, board);
			}
			_selectedPiece = null;
		}

		private void ExecuteMove(Piece piece, Position startPos, Position endPos, Piece[,] board)
		{
			Piece capturedPiece = board[endPos.Y, endPos.X];
			board[endPos.Y, endPos.X] = piece;
			board[startPos.Y, startPos.X] = null;
			piece.UpdatePosition(endPos);

			if (piece is King)
			{
				GameControler.Instance.UpdateKingPosition(endPos, piece.IsWhite);
			}
			else
			{
				Position kingPos = GameControler.Instance.GetKingPosition(piece.IsWhite);
				if (GameControler.Instance.IsSquareUnderAttack(kingPos, !piece.IsWhite))
				{
					board[startPos.Y, startPos.X] = piece;
					board[endPos.Y, endPos.X] = capturedPiece;
					piece.UpdatePosition(startPos);
					return;
				}
			}

			GameControler.Instance.LastMove = (startPos, endPos);
			GameControler.Instance.IsWhiteTurn = !GameControler.Instance.IsWhiteTurn;
			GameControler.Instance.CheckGameState();
			_networkClient?.SendMove(startPos, endPos);
		}
	}
}