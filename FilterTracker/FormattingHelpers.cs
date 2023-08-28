using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FilterTracker
{
	public static class FormattingHelpers
	{
		public static string GetFileSizeString(string filesize)
		{
			int fs;
			if (int.TryParse(filesize, out fs))
			{
				int mb = 1024 * 1024;
				int kb = 1024;
				if (fs > mb)
				{
					filesize = ((float)fs / (float)mb).ToString("N3") + " Mb";
				}
				else if (fs > kb)
				{
					filesize = ((float)fs / (float)kb).ToString("N3") + " Kb";
				}
				else
				{
					filesize += " bytes";
				}

			}

			return filesize;
		}

		public static string GetFileSizeString(int filesize)
		{
			string returned = "";
			int mb = 1024 * 1024;
			int kb = 1024;

			if (filesize > mb)
			{
				returned = ((float)filesize / (float)mb).ToString("N3") + " Mb";
			}
			else if (filesize > kb)
			{
				returned = ((float)filesize / (float)kb).ToString("N3") + " Kb";
			}
			else
			{
				returned = filesize.ToString() + " bytes";
			}


			return returned;
		}
	}
}