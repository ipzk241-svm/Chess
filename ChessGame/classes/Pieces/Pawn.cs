using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace ChessGame.Classes.Pieces
{
	public class Pawn : Piece
	{
		public Pawn(Position position, bool isWhite) : base(position, isWhite)
		{
			Name = "Pawn";
			string imagePath = isWhite ? "assets/WhitePawn.png" : "assets/BlackPawn.png";
			Icon = Image.FromFile(imagePath);
		}

		public override bool IsValidMove(Position endPos, Piece[,] board, bool isCheckEvaluation = false)
		{
			if (Position.Equals(endPos))
				return false;

			int direction = IsWhite ? -1 : 1;
			int deltaCol = endPos.X - Position.X;
			int deltaRow = endPos.Y - Position.Y;

			if (!isCheckEvaluation)
			{
				if (deltaCol == 0 && deltaRow == 2 * direction && board[endPos.Y, endPos.X] == null)
				{
					if ((IsWhite && Position.Y == 6) || (!IsWhite && Position.Y == 1))
					{
						int middleRow = Position.Y + direction;
						if (board[middleRow, Position.X] == null)
						{
							return true;
						}
					}
				}

				if (deltaCol == 0 && deltaRow == direction && board[endPos.Y, endPos.X] == null)
				{
					return true;
				}
			}

			if (Math.Abs(deltaCol) == 1 && deltaRow == direction)
			{
				if (isCheckEvaluation)
				{
					return true;
				}
				else
				{
					if (board[endPos.Y, endPos.X] != null && board[endPos.Y, endPos.X].IsWhite != IsWhite)
					{
						return true;
					}
				}
			}

			return false;
		}
	}
}
