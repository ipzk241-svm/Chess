using ChessClassLibrary;
using ChessGame.Classes;
using ChessGame.Classes.Pieces;
using System.Xml.Linq;

public class Bishop : Piece
{
	public Bishop(Position position, bool isWhite) : base(position, isWhite)
	{
		Name = "Bishop";
		string imagePath = isWhite ? "assets/WhiteBishop.png" : "assets/BlackBishop.png";
		Icon = Image.FromFile(imagePath);
	}

	public override bool IsValidMove(Position endPos, Piece[,] board, bool isCheckEvaluation = false)
	{
		if (Position.Equals(endPos))
			return false;
		if (Math.Abs(endPos.X - Position.X) != Math.Abs(endPos.Y - Position.Y))
			return false;

		int stepCol = endPos.X > Position.X ? 1 : -1;
		int stepRow = endPos.Y > Position.Y ? 1 : -1;
		int col = Position.X + stepCol;
		int row = Position.Y + stepRow;

		while (true)
		{
			if (col == endPos.X && row == endPos.Y)
				break;

			if (col < 0 || col >= 8 || row < 0 || row >= 8)
				return false;

			if (board[row, col] != null)
				return false;

			col += stepCol;
			row += stepRow;
		}

		return true;
	}
}