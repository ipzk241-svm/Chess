using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessServer
{
	public enum LogLevel
	{
		Info,
		Warning,
		Error
	}

	public class Logger
	{
		private readonly string logFilePath;
		private readonly bool logToConsole;
		private readonly LogLevel minimumLevel;
		private static readonly object lockObj = new();

		private Logger(string logFilePath, bool logToConsole, LogLevel minLevel)
		{
			this.logFilePath = logFilePath;
			this.logToConsole = logToConsole;
			this.minimumLevel = minLevel;
		}

		public static LoggerBuilder CreateBuilder() => new LoggerBuilder();

		public void Log(LogLevel level, string message)
		{
			if (level < minimumLevel)
				return;

			string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			string formatted = $"[{timestamp}] [{level.ToString().ToUpper()}] {message}";

			lock (lockObj)
			{
				File.AppendAllText(logFilePath, formatted + Environment.NewLine);
			}

			if (logToConsole)
			{
				Console.WriteLine(formatted);
			}
		}

		public void Info(string msg) => Log(LogLevel.Info, msg);
		public void Warning(string msg) => Log(LogLevel.Warning, msg);
		public void Error(string msg) => Log(LogLevel.Error, msg);

		public class LoggerBuilder
		{
			private string? customPath;
			private bool logToConsole = true;
			private LogLevel minLevel = LogLevel.Info;

			public LoggerBuilder WithLogFile(string path)
			{
				customPath = path;
				return this;
			}

			public LoggerBuilder WithConsole(bool enabled)
			{
				logToConsole = enabled;
				return this;
			}

			public LoggerBuilder MinimumLevel(LogLevel level)
			{
				minLevel = level;
				return this;
			}

			public Logger Build()
			{
				string path = customPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", $"log_{DateTime.Now:yyyy-MM-dd}.txt");

				var directory = Path.GetDirectoryName(path);
				if (!string.IsNullOrWhiteSpace(directory))
				{
					Directory.CreateDirectory(directory);
				}

				return new Logger(path, logToConsole, minLevel);
			}

		}
	}

}
