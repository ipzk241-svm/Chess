using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessServer
{
	public class Position
	{
		public int X { get; set; }
		public int Y { get; set; }
	}
	public class MoveRecord
	{
		public Position From { get; set; } = new();
		public Position To { get; set; } = new();
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;
		public string PlayerColor { get; set; } = string.Empty;
	}
	public class GameHistoryRecord
	{
		public string PlayerWhite { get; set; } = string.Empty;
		public string PlayerBlack { get; set; } = string.Empty;
		public List<MoveRecord> Moves { get; set; } = new();
	}

}
