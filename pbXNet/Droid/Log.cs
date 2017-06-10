using System;
using System.Runtime.InteropServices;

namespace pbXNet
{
	public class AndroidUtilLogLogger : ILogger
	{
		string _tag;

		public AndroidUtilLogLogger(string tag = null)
		{
			_tag = tag;
		}

		public void L(DateTime dt, LogType type, string msg)
		{
			msg = $"{dt.ToString("yyyy-M-d H:m:s.fff")}: {type}: {msg}";
			Android.Util.Log.WriteLine((Android.Util.LogPriority)type, _tag, msg);
		}
	}
}
