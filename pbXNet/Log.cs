using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace pbXNet
{
	public enum LogType
	{
		Debug = 3,
		Info = 4,
		Warning = 5,
		Error = 6,
	}

	public interface ILogger
	{
		void L(DateTime dt, LogType type, string msg);
	}

	public class SystemDiagnosticsDebugLogger : ILogger
	{
		public void L(DateTime dt, LogType type, string msg)
		{
#if DEBUG
			msg = $"{type}: {msg}";
			System.Diagnostics.Debug.WriteLine(dt.ToString("yyyy-M-d H:m:s.fff") + ": " + msg);
#endif
		}
	}

	public class ConsoleLogger : ILogger
	{
		public void L(DateTime dt, LogType type, string msg)
		{
			ConsoleColor c = Console.ForegroundColor;
			try
			{
				if (type == LogType.Error)
					Console.ForegroundColor = ConsoleColor.Red;
				else if (type == LogType.Warning)
					Console.ForegroundColor = ConsoleColor.Cyan;

				Console.Write(dt.ToString("yyyy-M-d H:m:s.fff") + ": " + $"{type}: ");
			}
			catch { }
			finally
			{
				Console.ForegroundColor = c;
			}
			try
			{
				Console.WriteLine(msg);
			}
			catch { }
		}
	}

	public static class Log
	{
		public static bool UseFullCallerTypeName { get; set; } = false;

		static Lazy<IList<ILogger>> _loggers = new Lazy<IList<ILogger>>(() => new List<ILogger>());

		public static void AddLogger(ILogger logger)
		{
			_loggers.Value.Add(logger);
		}

		public static void L(LogType type, string msg, object caller = null, [CallerMemberName]string callerName = null)
		{
			DateTime dt = DateTime.Now;

			string callerInfo = null;
			if (caller != null)
			{
				if(caller is Type callerType)
					callerInfo = !UseFullCallerTypeName ? callerType.Name : callerType.FullName;
				else
					callerInfo = !UseFullCallerTypeName ? caller.GetType().Name : caller.GetType().FullName;
			}

			callerInfo = (callerInfo != null ? (callerInfo + ": ") : ("")) + callerName;

			msg = $"{callerInfo}: {msg}";

#if DEBUG
			if (_loggers.Value.Count <= 0)
				AddLogger(new SystemDiagnosticsDebugLogger());
#endif

			foreach (var l in _loggers.Value)
			{
				l.L(dt, type, msg);
			}
		}

		public static string L(LogType type, Exception ex, object caller = null, [CallerMemberName]string callerName = null)
		{
			string message = ex.Message;
			if (ex.InnerException != null)
				message += $" {ex.InnerException.Message + (ex.InnerException.Message.EndsWith(".") ? "" : ".")}";
			L(type, message, caller, callerName);
			return message;
		}

		public static void E(string msg, object caller = null, [CallerMemberName]string callerName = null)
		{
			L(LogType.Error, msg, caller, callerName);
		}

		public static string E(Exception ex, object caller = null, [CallerMemberName]string callerName = null)
		{
			return L(LogType.Error, ex, caller, callerName);
		}

		public static void W(string msg, object caller = null, [CallerMemberName]string callerName = null)
		{
			L(LogType.Warning, msg, caller, callerName);
		}

		public static string W(Exception ex, object caller = null, [CallerMemberName]string callerName = null)
		{
			return L(LogType.Warning, ex, caller, callerName);
		}

		public static void I(string msg, object caller = null, [CallerMemberName]string callerName = null)
		{
			L(LogType.Info, msg, caller, callerName);
		}

		public static string I(Exception ex, object caller = null, [CallerMemberName]string callerName = null)
		{
			return L(LogType.Info, ex, caller, callerName);
		}

		public static void D(string msg, object caller = null, [CallerMemberName]string callerName = null)
		{
#if DEBUG
			L(LogType.Debug, msg, caller, callerName);
#endif
		}

		public static string D(Exception ex, object caller = null, [CallerMemberName]string callerName = null)
		{
#if DEBUG
			return L(LogType.Debug, ex, caller, callerName);
#else
			return null;
#endif
		}
	}
}
