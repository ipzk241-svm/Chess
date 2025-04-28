using System.Drawing;

namespace ChessGame.Classes.Pieces
{
	public abstract class Piece
	{
		public bool IsWhite { get; set; }
		public string Name { get; set; }
		public Image Icon { get; set; }
		public Position Position { get; private set; }

		protected Piece(Position position, bool isWhite)
		{
			Position = position;
			IsWhite = isWhite;
		}

		public void UpdatePosition(Position newPosition)
		{
			Position = newPosition;
		}

		public abstract bool IsValidMove(Position endPos, Piece[,] board, bool isCheckEvaluation = false);
	}
}