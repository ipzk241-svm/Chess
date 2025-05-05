using ChessGame.Classes;
using ChessGame.Classes.Pieces;
using System.Xml.Linq;

public class Knight : Piece
{
	public Knight(Position position, bool isWhite) : base(position, isWhite)
	{
		Name = "Knight";
		string imagePath = isWhite ? "assets/WhiteKnight.png" : "assets/BlackKnight.png";
		Icon = Image.FromFile(imagePath);
	}

	public override bool IsValidMove(Position endPos, Piece[,] board, bool isCheckEvaluation = false)
	{
		if (Position.Equals(endPos))
			return false;

		int deltaCol = Math.Abs(endPos.X - Position.X);
		int deltaRow = Math.Abs(endPos.Y - Position.Y);
		return (deltaCol == 2 && deltaRow == 1) || (deltaCol == 1 && deltaRow == 2);
	}
}