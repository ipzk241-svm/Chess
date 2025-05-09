using ChessClassLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessServer
{
	public class MoveRecord
	{
		public Position From { get; set; }
		public Position To { get; set; }
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
