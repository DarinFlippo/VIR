using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using log4net;

namespace FilterTracker
{
	public static class Logger
	{
		private static ILog _log;
		public static ILog Log
		{
			get
			{
				if (_log == null)
				{
					_log = LogManager.GetLogger("FilterTracker");
				}
				return _log;
			}
		}

		public static void LogException(Exception ex, string msg = null)
		{
			if (ex == null)
				return;

			if (!string.IsNullOrEmpty(msg))
				Log.Error($"{msg}\r\n{ex.Message}", ex);
			else
				Log.Error(ex.Message, ex);

			var iex = ex.InnerException;
			while (iex != null)
			{
				Log.Error(iex.Message, iex);
				iex = iex.InnerException;
			}

		}
	}


}