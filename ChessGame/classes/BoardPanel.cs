using ChessGame.Classes.Pieces;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChessGame.Classes
{
	public class BoardPanel : Panel
	{
		private const int Size = 8;
		private int cellSize = 60;
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
			var board = GameControler.Instance.Board;

			for (int row = 0; row < Size; row++)
			{
				for (int col = 0; col < Size; col++)
				{
					Brush brush = (row + col) % 2 == 0 ? Brushes.White : Brushes.Gray;
					g.FillRectangle(brush, col * cellSize, row * cellSize, cellSize, cellSize);

					if (GameControler.Instance.LastMove.HasValue)
					{
						var (fromPos, toPos) = GameControler.Instance.LastMove.Value;
						if ((row == fromPos.Y && col == fromPos.X) || (row == toPos.Y && col == toPos.X))
						{
							g.FillRectangle(new SolidBrush(Color.YellowGreen), col * cellSize, row * cellSize, cellSize, cellSize);
						}
					}

					if (board[row, col] is King && board[row, col].IsWhite == GameControler.Instance.IsWhiteTurn && GameControler.Instance.IsKingInCheck(GameControler.Instance.IsWhiteTurn))
					{
						g.FillRectangle(new SolidBrush(Color.MediumVioletRed), col * cellSize, row * cellSize, cellSize, cellSize);
					}

					if (selectedPiece != null && selectedPiece.Y == row && selectedPiece.X == col)
					{
						g.FillRectangle(new SolidBrush(Color.Yellow), col * cellSize, row * cellSize, cellSize, cellSize);
					}

					if (selectedPiece != null)
					{
						Position startPos = selectedPiece;
						Position endPos = new(col, row);
						Piece piece = board[startPos.Y, startPos.X];
						if (piece != null && piece.IsValidMove(endPos, board) &&
							(board[row, col] == null || board[row, col].IsWhite != piece.IsWhite))
						{
							bool isKingMove = piece is King;
							bool isKingInCheck = GameControler.Instance.IsKingInCheck(piece.IsWhite);

							if (isKingMove)
							{
								bool isTargetSafe = !GameControler.Instance.IsSquareUnderAttack(endPos, !piece.IsWhite);
								if (isTargetSafe)
								{
									g.FillRectangle(new SolidBrush(Color.Green), col * cellSize, row * cellSize, cellSize, cellSize);
								}
							}
							else
							{
								if (!isKingInCheck || GameControler.Instance.IsMoveResolvingCheck(startPos, endPos, piece.IsWhite))
								{
									if (board[row, col] != null)
									{
										g.FillRectangle(new SolidBrush(Color.Red), col * cellSize, row * cellSize, cellSize, cellSize);
									}
									else
									{
										g.FillRectangle(new SolidBrush(Color.LightGreen), col * cellSize, row * cellSize, cellSize, cellSize);
									}
								}
							}
						}
					}

					g.DrawRectangle(Pens.Black, col * cellSize, row * cellSize, cellSize, cellSize);

					if (board[row, col] != null)
					{
						g.DrawImage(board[row, col].Icon, col * cellSize + 5, row * cellSize + 5, 50, 50);
					}
				}
			}
		}

		private void BoardPanel_MouseClick(object sender, MouseEventArgs e)
		{
			if (GameControler.Instance.IsWhiteTurn != networkClient.IsLocalPlayerWhite)
				return;
			if (GameControler.Instance.GameEnded) return;

			int clickedRow = e.Y / cellSize;
			int clickedCol = e.X / cellSize;

			if (clickedRow < 0 || clickedRow >= Size || clickedCol < 0 || clickedCol >= Size)
				return;

			var board = GameControler.Instance.Board;

			if (selectedPiece == null)
			{
				if (board[clickedRow, clickedCol] != null && board[clickedRow, clickedCol].IsWhite == GameControler.Instance.IsWhiteTurn)
				{
					selectedPiece = new Position(clickedCol, clickedRow);
					Invalidate();
				}
			}
			else
			{
				Position startPos = selectedPiece;
				Position endPos = new Position(clickedCol, clickedRow);
				Piece piece = board[startPos.Y, startPos.X];

				if (endPos.X >= 0 && endPos.X < Size && endPos.Y >= 0 && endPos.Y < Size &&
					piece.IsValidMove(endPos, board) &&
					(board[endPos.Y, endPos.X] == null || board[endPos.Y, endPos.X].IsWhite != piece.IsWhite))
				{
					bool isKingMove = piece is King;
					bool isKingInCheck = GameControler.Instance.IsKingInCheck(piece.IsWhite);

					if (isKingMove)
					{
						bool isTargetSafe = !GameControler.Instance.IsSquareUnderAttack(endPos, !piece.IsWhite);
						if (isTargetSafe)
						{
							Piece capturedPiece = board[endPos.Y, endPos.X];
							board[endPos.Y, endPos.X] = piece;
							board[startPos.Y, startPos.X] = null;
							piece.UpdatePosition(endPos);
							GameControler.Instance.UpdateKingPosition(endPos, piece.IsWhite);
							GameControler.Instance.LastMove = (startPos, endPos);
							GameControler.Instance.IsWhiteTurn = !GameControler.Instance.IsWhiteTurn;
							GameControler.Instance.CheckGameState(this);
							networkClient?.SendMove(startPos, endPos);

						}
					}
					else
					{
						if (!isKingInCheck || GameControler.Instance.IsMoveResolvingCheck(startPos, endPos, piece.IsWhite))
						{
							Piece capturedPiece = board[endPos.Y, endPos.X];
							board[endPos.Y, endPos.X] = piece;
							board[startPos.Y, startPos.X] = null;
							piece.UpdatePosition(endPos);

							Position kingPos = GameControler.Instance.GetKingPosition(piece.IsWhite);
							bool wouldBeInCheck = GameControler.Instance.IsSquareUnderAttack(kingPos, !piece.IsWhite);

							if (!wouldBeInCheck)
							{
								GameControler.Instance.LastMove = (startPos, endPos);
								GameControler.Instance.IsWhiteTurn = !GameControler.Instance.IsWhiteTurn;
								GameControler.Instance.CheckGameState(this);
								networkClient?.SendMove(startPos, endPos);
							}
							else
							{
								board[startPos.Y, startPos.X] = piece;
								board[endPos.Y, endPos.X] = capturedPiece;
								piece.UpdatePosition(startPos);

							}
						}
					}
				}
				selectedPiece = null;
				Invalidate();
			}
		}
	}
}