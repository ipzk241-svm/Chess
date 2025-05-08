using ChessGame.Classes.Pieces;
using ChessGame.interfaces;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChessGame.Classes
{
	public class BoardPanel : Panel, IBoardPanel
	{
		private const int BoardSize = 8;
		private readonly int _cellSize;
		private IGameMediator _mediator;

		public BoardPanel(int cellSize = 60)
		{
			_cellSize = cellSize;
			this.Width = cellSize * BoardSize;
			this.Height = cellSize * BoardSize;
			this.DoubleBuffered = true;
			this.Paint += Board_Paint;
			this.MouseClick += BoardPanel_MouseClick;
		}

		public void SetMediator(IGameMediator mediator) =>
			_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

		private void Board_Paint(object sender, PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			_mediator.ApplyBoardRotation(g, Width, Height);
			var board = _mediator.GetBoard();

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

		private void DrawCellBackground(Graphics g, int row, int col, Piece[,] board)
		{
			Brush backgroundBrush = (row + col) % 2 == 0 ? Brushes.White : Brushes.Gray;
			Rectangle cellRect = new Rectangle(col * _cellSize, row * _cellSize, _cellSize, _cellSize);
			Position pos = new Position(col, row);

			if (_mediator.IsLastMovePosition(pos))
				backgroundBrush = new SolidBrush(Color.YellowGreen);
			if (IsKingInCheck(row, col, board))
				backgroundBrush = new SolidBrush(Color.MediumVioletRed);
			if (_mediator.IsSelectedPiece(pos))
				backgroundBrush = new SolidBrush(Color.Yellow);
			Position selectedPiece = _mediator.GetSelectedPiece();
			if (selectedPiece != null && _mediator.IsValidMoveTarget(selectedPiece, pos))
				backgroundBrush = GetMoveHighlightBrush(row, col, board);

			g.FillRectangle(backgroundBrush, cellRect);
		}

		private bool IsKingInCheck(int row, int col, Piece[,] board)
		{
			return board[row, col] is King &&
				   board[row, col].IsWhite == _mediator.GetIsWhiteTurn() &&
				   _mediator.IsKingInCheck(board[row, col].IsWhite);
		}

		private Brush GetMoveHighlightBrush(int row, int col, Piece[,] board)
		{
			Position pos = new Position(col, row);
			Piece piece = board[_mediator.GetSelectedPiece().Y, _mediator.GetSelectedPiece().X];
			return piece is King ? new SolidBrush(Color.Green) :
				   board[row, col] != null ? new SolidBrush(Color.Red) :
				   new SolidBrush(Color.LightGreen);
		}

		private void DrawPieceIcon(Graphics g, int row, int col, Piece[,] board)
		{
			if (board[row, col] == null) return;

			Image icon = board[row, col].Icon;
			if (_mediator.ShouldRotateIcons())
			{
				icon = (Image)icon.Clone();
				icon.RotateFlip(RotateFlipType.Rotate180FlipNone);
			}
			g.DrawImage(icon, col * _cellSize + 5, row * _cellSize + 5, 50, 50);
		}

		private void BoardPanel_MouseClick(object sender, MouseEventArgs e)
		{
			if (!_mediator.CanProcessClick(_mediator.GetIsWhiteTurn())) return;

			var (clickedRow, clickedCol) = CalculateClickPosition(e);
			if (!IsValidPosition(clickedRow, clickedCol)) return;

			Position clickedPos = new Position(clickedCol, clickedRow);
			Position selectedPiece = _mediator.GetSelectedPiece();

			if (selectedPiece == null)
			{
				_mediator.TrySelectPiece(clickedPos);
			}
			else
			{
				_mediator.HandleLocalMove(selectedPiece, clickedPos);
			}
			Invalidate();
		}

		private (int row, int col) CalculateClickPosition(MouseEventArgs e)
		{
			return _mediator.ShouldRotateIcons()
				? ((Height - e.Y) / _cellSize, (Width - e.X) / _cellSize)
				: (e.Y / _cellSize, e.X / _cellSize);
		}

		private bool IsValidPosition(int row, int col)
		{
			return row >= 0 && row < BoardSize && col >= 0 && col < BoardSize;
		}
	}
}