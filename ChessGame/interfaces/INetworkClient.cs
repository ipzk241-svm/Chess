using ChessClassLibrary;
using ChessGame.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessGame.interfaces
{
	public interface INetworkClient
	{
		bool IsLocalPlayerWhite { get; }
		event Action<string> OpponentNameReceived;
		event Action<string> DisconnectAction;
		event Action<Position, Position> MoveReceived;

		void SendMove(Position from, Position to);
		void SendLeave();
		void Disconnect();
	}
}
