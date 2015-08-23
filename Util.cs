using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.IO;

namespace Axiom
{
	public static class Util
	{

		private static StringBuilder sbprintf(string text, params object[] args)
		{
			if (string.IsNullOrEmpty(text)) return new StringBuilder();
			StringBuilder sb = new StringBuilder(text.Length);
			return sb;
		}

		public static void printf(string text, params object[] args)
		{

		}

		public static void sprintf(Stream s, string text, params object[] args)
		{

		}
	}

}
