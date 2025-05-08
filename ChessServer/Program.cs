using ChessServer;

var logger = Logger.CreateBuilder()
				   .WithConsole(true)
				   .WithLogFile("log.txt")
				   .Build();

Server server = new Server(logger);
server.Start(5000);
Console.ReadLine();

