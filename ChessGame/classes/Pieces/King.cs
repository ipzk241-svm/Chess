using ChessGame.Classes;
using ChessGame.Classes.Pieces;
using System.Xml.Linq;

public class King : Piece
{
	public King(Position position, bool isWhite) : base(position, isWhite)
	{
		Name = "King";
		string imagePath = isWhite ? "assets/WhiteKing.png" : "assets/BlackKing.png";
		Icon = Image.FromFile(imagePath);
	}

	public override bool IsValidMove(Position endPos, Piece[,] board, bool isCheckEvaluation = false)
	{
		if (Position.Equals(endPos))
			return false;

		int deltaCol = Math.Abs(endPos.X - Position.X);
		int deltaRow = Math.Abs(endPos.Y - Position.Y);
		return deltaCol <= 1 && deltaRow <= 1 && (deltaCol != 0 || deltaRow != 0);
	}
}