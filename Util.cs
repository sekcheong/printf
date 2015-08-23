using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;

namespace Axiom
{
	public static class Util
	{
		//http://www.cplusplus.com/reference/cstdio/printf/
		//%[flags][width][.precision][length]specifier
		
		private static string FORMAT_SPECIFIERS = @"%(\d+\$)?(-+ 0)?(\d+)?(\.\d+)?([diuoxXfFeEgGaAcsp%])|%t\[.+\]";
		private static Regex _formatRegex = new Regex(FORMAT_SPECIFIERS, RegexOptions.Compiled);


		[FlagsAttribute]
		private static enum Flags
		{
			LEFT_JUSTIFY = 0,        //-
			LEADING_ZERO_FILL = 1,   //0
			PRINT_PLUS = 2,			 //+
			INVISIBLE_PLUS_SIGN = 4  //<space>
		}


		private static bool ParseFormatString(string format, out List<string> tokens, out List<int> fmtIndexes)
		{
			tokens = null;
			fmtIndexes = null;
			MatchCollection matches = _formatRegex.Matches(format);
			if (matches.Count == 0) return false;

			int lastEnd = 0;
			tokens = new List<string>();
			fmtIndexes = new List<int>();

			foreach (Match m in matches) {
				if (lastEnd < m.Index) {
					tokens.Add(format.Substring(lastEnd, m.Index - lastEnd));
				}
				tokens.Add(m.Value);
				fmtIndexes.Add(tokens.Count - 1);
				lastEnd = m.Index + m.Value.Length;
			}

			if (lastEnd < format.Length) {
				tokens.Add(format.Substring(lastEnd));
			}

			return true;
		}


		private static string formatValue(string format, object value)
		{
			return value.ToString();
		}


		private static List<string> sbprintf(string format, object[] args)
		{
			List<string> tokens;
			List<int> fmtIndexes;
			int count = 0;

			if (!ParseFormatString(format, out tokens, out fmtIndexes)) return null;

			if (fmtIndexes.Count > args.Length) throw new InvalidOperationException("Insufficient number of arguments");

			foreach (int q in fmtIndexes) {
				tokens[q] = formatValue(tokens[q], args[count]);
				count++;
			}
			Console.WriteLine("");
			return null;
		}


		public static int printf(string format, params object[] args)
		{
			string str = sprintf(format, args);
			if (string.IsNullOrEmpty(str)) return 0;
			Console.Write(str);
			return str.Length;
		}


		public static string sprintf(string format, params object[] args)
		{
			if (string.IsNullOrEmpty(format)) return null;
			List<string> pieces = sbprintf(format, args);
			if (pieces == null || pieces.Count == 0) return null;
			return string.Join(null, pieces);
		}


		public static int fprintf(Stream s, string format, params object[] args)
		{
			return fprintf(s, Encoding.Default, format, args);
		}


		public static int fprintf(Stream s, Encoding encoding, string format, params object[] args)
		{
			string str = sprintf(format, args);
			byte[] data = encoding.GetBytes(str);
			s.Write(data, 0, data.Length);
			return data.Length;
		}
	}

}