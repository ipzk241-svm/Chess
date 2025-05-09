using ChessClassLibrary;
using ChessGame.Classes;
using ChessGame.Classes.Pieces;
using System.Xml.Linq;

public class Rook : Piece
{
	public Rook(Position position, bool isWhite) : base(position, isWhite)
	{
		Name = "Rook";
		string imagePath = isWhite ? "assets/WhiteRook.png" : "assets/BlackRook.png";
		Icon = Image.FromFile(imagePath);
	}

	public override bool IsValidMove(Position endPos, Piece[,] board, bool isCheckEvaluation = false)
	{
		if (Position.Equals(endPos))
			return false;

		if (Position.X != endPos.X && Position.Y != endPos.Y)
			return false;

		int stepCol = Position.X == endPos.X ? 0 : (endPos.X > Position.X ? 1 : -1);
		int stepRow = Position.Y == endPos.Y ? 0 : (endPos.Y > Position.Y ? 1 : -1);
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
