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

		/// <summary>
		/// Initializes a new instance of the <see cref="BoardPanel"/> class.
		/// </summary>
		/// <param name="cellSize">The size of each cell on the board in pixels. Default is 60.</param>
		public BoardPanel(int cellSize = 60)
		{
			_cellSize = cellSize;
			this.Width = cellSize * BoardSize;
			this.Height = cellSize * BoardSize;
			this.DoubleBuffered = true;
			this.Paint += Board_Paint;
			this.MouseClick += BoardPanel_MouseClick;
		}

		/// <summary>
		/// Sets the mediator that handles game logic for this board panel.
		/// </summary>
		/// <param name="mediator">The game mediator responsible for managing game state and logic.</param>
		/// <exception cref="ArgumentNullException">Thrown when the mediator is null.</exception>
		public void SetMediator(IGameMediator mediator) =>
			_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

		/// <summary>
		/// Handles the painting of the chess board, rendering cells and pieces.
		/// </summary>
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

		/// <summary>
		/// Draws the background of a cell, applying highlights based on the game state (e.g., selected, check, last move).
		/// </summary>
		/// <param name="g">The graphics context used for drawing.</param>
		/// <param name="row">The row index of the cell (0 to 7).</param>
		/// <param name="col">The column index of the cell (0 to 7).</param>
		/// <param name="board">The 2D array representing the chess board with pieces.</param>
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

		/// <summary>
		/// Determines if the king at the specified position is in check.
		/// </summary>
		/// <param name="row">The row index of the cell (0 to 7).</param>
		/// <param name="col">The column index of the cell (0 to 7).</param>
		/// <param name="board">The 2D array representing the chess board with pieces.</param>
		/// <returns>True if the king at the position is in check; otherwise, false.</returns>
		private bool IsKingInCheck(int row, int col, Piece[,] board)
		{
			return board[row, col] is King &&
				   board[row, col].IsWhite == _mediator.GetIsWhiteTurn() &&
				   _mediator.IsKingInCheck(board[row, col].IsWhite);
		}

		/// <summary>
		/// Returns the appropriate brush for highlighting possible move targets of a selected piece.
		/// </summary>
		/// <param name="row">The row index of the cell (0 to 7).</param>
		/// <param name="col">The column index of the cell (0 to 7).</param>
		/// <param name="board">The 2D array representing the chess board with pieces.</param>
		/// <returns>A brush used to highlight the move target (e.g., green for empty cells, red for captures).</returns>
		private Brush GetMoveHighlightBrush(int row, int col, Piece[,] board)
		{
			Position pos = new Position(col, row);
			Piece piece = board[_mediator.GetSelectedPiece().Y, _mediator.GetSelectedPiece().X];
			return piece is King ? new SolidBrush(Color.Green) :
				   board[row, col] != null ? new SolidBrush(Color.Red) :
				   new SolidBrush(Color.LightGreen);
		}

		/// <summary>
		/// Draws the icon of a piece at the specified cell, applying rotation if needed.
		/// </summary>
		/// <param name="g">The graphics context used for drawing.</param>
		/// <param name="row">The row index of the cell (0 to 7).</param>
		/// <param name="col">The column index of the cell (0 to 7).</param>
		/// <param name="board">The 2D array representing the chess board with pieces.</param>
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

		/// <summary>
		/// Handles mouse clicks on the board to select or move pieces.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="MouseEventArgs"/> containing the mouse click information.</param>
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

		/// <summary>
		/// Calculates the row and column of the cell based on the mouse click position.
		/// </summary>
		/// <returns>A tuple containing the row and column indices of the clicked cell.</returns>
		private (int row, int col) CalculateClickPosition(MouseEventArgs e)
		{
			return _mediator.ShouldRotateIcons()
				? ((Height - e.Y) / _cellSize, (Width - e.X) / _cellSize)
				: (e.Y / _cellSize, e.X / _cellSize);
		}

		/// <summary>
		/// Checks if the specified row and column are within the valid boundaries of the chess board.
		/// </summary>
		/// <param name="row">The row index to check (0 to 7).</param>
		/// <param name="col">The column index to check (0 to 7).</param>
		/// <returns>True if the position is valid; otherwise, false.</returns>
		private bool IsValidPosition(int row, int col)
		{
			return row >= 0 && row < BoardSize && col >= 0 && col < BoardSize;
		}
	}
}