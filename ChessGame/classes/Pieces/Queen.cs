using ChessGame.Classes;
using ChessGame.Classes.Pieces;
using System.Xml.Linq;

public class Queen : Piece
{
	public Queen(Position position, bool isWhite) : base(position, isWhite)
	{
		Name = "Queen";
		string imagePath = isWhite ? "assets/WhiteQueen.png" : "assets/BlackQueen.png";
		Icon = Image.FromFile(imagePath);
	}

	public override bool IsValidMove(Position endPos, Piece[,] board, bool isCheckEvaluation = false)
	{
		if (Position.Equals(endPos))
			return false;

		bool movesLikeRook = Position.X == endPos.X || Position.Y == endPos.Y;
		bool movesLikeBishop = Math.Abs(endPos.X - Position.X) == Math.Abs(endPos.Y - Position.Y);

		if (!movesLikeRook && !movesLikeBishop)
			return false;

		int stepCol = 0, stepRow = 0;
		if (movesLikeRook)
		{
			stepCol = Position.X == endPos.X ? 0 : (endPos.X > Position.X ? 1 : -1);
			stepRow = Position.Y == endPos.Y ? 0 : (endPos.Y > Position.Y ? 1 : -1);
		}
		else if (movesLikeBishop)
		{
			stepCol = endPos.X > Position.X ? 1 : -1;
			stepRow = endPos.Y > Position.Y ? 1 : -1;
		}

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