using ChessGame.Classes.Pieces;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChessGame.Classes
{
	public class BoardPanel : Panel
	{
		private const int Size = 8;
		private readonly int cellSize;
		private Position selectedPiece = null;
		private NetworkClient networkClient;

		public BoardPanel(int cellSize = 60)
		{
			this.cellSize = cellSize;
			this.Width = cellSize * Size;
			this.Height = cellSize * Size;
			this.DoubleBuffered = true;
			this.Paint += Board_Paint;
			this.MouseClick += BoardPanel_MouseClick;
		}

		public void SetNetworkClient(NetworkClient client) => networkClient = client;

		private void Board_Paint(object sender, PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			if (!networkClient.IsLocalPlayerWhite)
			{
				g.TranslateTransform(Width, Height);
				g.RotateTransform(180);
			}

			DrawBoard(g);
			DrawHighlights(g);
			DrawPieces(g);
		}

		private void DrawBoard(Graphics g)
		{
			for (int row = 0; row < Size; row++)
			{
				for (int col = 0; col < Size; col++)
				{
					Brush baseBrush = (row + col) % 2 == 0 ? Brushes.White : Brushes.Gray;
					FillCell(g, baseBrush, row, col);
					g.DrawRectangle(Pens.Black, GetCellRect(row, col));
				}
			}
		}

		private void DrawHighlights(Graphics g)
		{
			var board = GameControler.Instance.Board;

			if (GameControler.Instance.LastMove.HasValue)
			{
				var (from, to) = GameControler.Instance.LastMove.Value;
				FillCell(g, Brushes.YellowGreen, from.Y, from.X);
				FillCell(g, Brushes.YellowGreen, to.Y, to.X);
			}

			for (int row = 0; row < Size; row++)
			{
				for (int col = 0; col < Size; col++)
				{
					var piece = board[row, col];
					if (piece is King king && king.IsWhite == GameControler.Instance.IsWhiteTurn &&
						GameControler.Instance.IsKingInCheck(king.IsWhite))
					{
						FillCell(g, Brushes.MediumVioletRed, row, col);
					}
				}
			}

			if (selectedPiece != null)
			{
				FillCell(g, Brushes.Yellow, selectedPiece.Y, selectedPiece.X);
				ShowValidMoves(g, selectedPiece);
			}
		}

		private void ShowValidMoves(Graphics g, Position from)
		{
			var board = GameControler.Instance.Board;
			var piece = board[from.Y, from.X];

			for (int row = 0; row < Size; row++)
			{
				for (int col = 0; col < Size; col++)
				{
					var to = new Position(col, row);
					if (!piece.IsValidMove(to, board)) continue;
					if (board[row, col]?.IsWhite == piece.IsWhite) continue;

					bool isKing = piece is King;
					bool isInCheck = GameControler.Instance.IsKingInCheck(piece.IsWhite);

					if (isKing)
					{
						if (!GameControler.Instance.IsSquareUnderAttack(to, !piece.IsWhite))
							FillCell(g, Brushes.Green, row, col);
					}
					else if (!isInCheck || GameControler.Instance.IsMoveResolvingCheck(from, to, piece.IsWhite))
					{
						Brush brush = board[row, col] != null ? Brushes.Red : Brushes.LightGreen;
						FillCell(g, brush, row, col);
					}
				}
			}
		}

		private void DrawPieces(Graphics g)
		{
			var board = GameControler.Instance.Board;

			for (int row = 0; row < Size; row++)
			{
				for (int col = 0; col < Size; col++)
				{
					var piece = board[row, col];
					if (piece == null) return;

					Image icon = (Image)piece.Icon.Clone();
					if (!networkClient.IsLocalPlayerWhite)
						icon.RotateFlip(RotateFlipType.Rotate180FlipNone);

					g.DrawImage(icon, col * cellSize + 5, row * cellSize + 5, 50, 50);
				}
			}
		}

		private void BoardPanel_MouseClick(object sender, MouseEventArgs e)
		{
			if (GameControler.Instance.IsWhiteTurn != networkClient.IsLocalPlayerWhite || GameControler.Instance.GameEnded)
				return;

			int row = networkClient.IsLocalPlayerWhite ? e.Y / cellSize : (Height - e.Y) / cellSize;
			int col = networkClient.IsLocalPlayerWhite ? e.X / cellSize : (Width - e.X) / cellSize;

			if (row < 0 || row >= Size || col < 0 || col >= Size)
				return;

			if (selectedPiece == null)
				TrySelectPiece(row, col);
			else
				TryMovePiece(row, col);
		}

		private void TrySelectPiece(int row, int col)
		{
			var piece = GameControler.Instance.Board[row, col];
			if (piece != null && piece.IsWhite == GameControler.Instance.IsWhiteTurn)
			{
				selectedPiece = new Position(col, row);
				Invalidate();
			}
		}

		private void TryMovePiece(int row, int col)
		{
			var board = GameControler.Instance.Board;
			var from = selectedPiece;
			var to = new Position(col, row);
			var piece = board[from.Y, from.X];

			if (!piece.IsValidMove(to, board) || board[to.Y, to.X]?.IsWhite == piece.IsWhite)
			{
				selectedPiece = null;
				Invalidate();
				return;
			}

			if (piece is King && !GameControler.Instance.IsSquareUnderAttack(to, !piece.IsWhite))
			{
				MakeMove(piece, from, to);
			}
			else if (!GameControler.Instance.IsKingInCheck(piece.IsWhite) || GameControler.Instance.IsMoveResolvingCheck(from, to, piece.IsWhite))
			{
				Piece captured = board[to.Y, to.X];
				board[to.Y, to.X] = piece;
				board[from.Y, from.X] = null;
				piece.UpdatePosition(to);

				if (!GameControler.Instance.IsSquareUnderAttack(GameControler.Instance.GetKingPosition(piece.IsWhite), !piece.IsWhite))
				{
					FinalizeMove(from, to);
				}
				else
				{
					board[from.Y, from.X] = piece;
					board[to.Y, to.X] = captured;
					piece.UpdatePosition(from);
				}
			}

			selectedPiece = null;
			Invalidate();
		}

		private void MakeMove(Piece piece, Position from, Position to)
		{
			var board = GameControler.Instance.Board;
			board[to.Y, to.X] = piece;
			board[from.Y, from.X] = null;
			piece.UpdatePosition(to);

			if (piece is King king)
				GameControler.Instance.UpdateKingPosition(to, king.IsWhite);

			FinalizeMove(from, to);
		}

		private void FinalizeMove(Position from, Position to)
		{
			GameControler.Instance.LastMove = (from, to);
			GameControler.Instance.IsWhiteTurn = !GameControler.Instance.IsWhiteTurn;
			GameControler.Instance.CheckGameState();
			networkClient?.SendMove(from, to);
		}

		private void FillCell(Graphics g, Brush brush, int row, int col)
			=> g.FillRectangle(brush, col * cellSize, row * cellSize, cellSize, cellSize);

		private Rectangle GetCellRect(int row, int col)
			=> new Rectangle(col * cellSize, row * cellSize, cellSize, cellSize);
	}
}
