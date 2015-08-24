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
		//  2      3      4           5      6

		private static string FORMAT_SPECIFIERS = @"\%(\d*\$)?([\,\#\-\+ ]*)(\d*)(?:\.(\d+))?([hl])?([dioxXucsfeEgGpn%])|\%t\[.+\]";
		private static Regex _formatRegex = new Regex(FORMAT_SPECIFIERS, RegexOptions.Compiled);

		[FlagsAttribute]
		private enum Flags
		{
			NONE = 0,                      //<the default>
			LEFT_JUSTIFY = 1,              //-
			LEADING_ZERO_FILL = 2,         //0
			FORCE_SIGN = 4,			       //+
			INVISIBLE_PLUS_SIGN = 8,       //<space>
			GROUP_THOUSANDS = 16,          //, 
			ALTERNATE = 32                 //#
		}


		private enum SpecifierType
		{
			SIGNED_INT,               //d, i
			UNSIGNED_INT,             //u  
			UNSIGNED_OCT,             //o
			UNSINGED_HEX,             //h    
			UNSIGNED_HEX_UPPER,       //H  
			FLOAT,                    //f  
			FLOAT_UPPER,              //F
			SCIENTIFIC,               //e
			SCIENTIFIC_UPPER,         //E
			GENERAL,                  //g
			GENERAL_UPPER,            //G
			CHAR,                     //c
			STRING,                   //s
			PERCENT,                  //%
			DATE_TIME,                //t
		}

		private enum LengthType
		{
			NONE,
			SHORT,
			LONG
		}

		private class FormatSpecification
		{
			public Flags Flags { get; set; }
			public int Width { get; set; }
			public int Precision { get; set; }
			public LengthType Length { get; set; }
			public char PaddingChar { get; set; }
			public SpecifierType Specifier { get; set; }

			public string Format(object o)
			{
				switch (this.Specifier) {
					case SpecifierType.SIGNED_INT:
					case SpecifierType.UNSIGNED_INT:
					case SpecifierType.UNSIGNED_OCT:
					case SpecifierType.UNSINGED_HEX:
					case SpecifierType.UNSIGNED_HEX_UPPER:
					case SpecifierType.FLOAT:
					case SpecifierType.FLOAT_UPPER:
					case SpecifierType.SCIENTIFIC:
					case SpecifierType.SCIENTIFIC_UPPER:
					case SpecifierType.GENERAL:
					case SpecifierType.GENERAL_UPPER:
					case SpecifierType.CHAR:
					case SpecifierType.STRING:
					case SpecifierType.PERCENT:
					case SpecifierType.DATE_TIME:
						break;
				}
				return null;
			}
		}

		private static bool ParseFormatSpec(Match format, object[] args, int pos, out int argPos)
		{

			FormatSpecification fs = new FormatSpecification();
			argPos = pos;
			string str = format.Groups[2].Value;

			if (str.Contains("-")) fs.Flags |= Flags.LEFT_JUSTIFY;
			if (str.Contains("+")) fs.Flags |= Flags.FORCE_SIGN;
			if (str.Contains("#")) fs.Flags |= Flags.ALTERNATE;
			if (str.Contains(" ")) {
				if ((fs.Flags & Flags.FORCE_SIGN) == 0) {
					fs.Flags |= Flags.INVISIBLE_PLUS_SIGN;
				}
			}

			//parse the width field
			str = format.Groups[3].Value;
			if (!string.IsNullOrEmpty(str)) {
				if (str.StartsWith("0")) fs.Flags |= Flags.LEADING_ZERO_FILL;
				if (str == "*") {
					if (!(pos < args.Length) || !(args[pos] is int)) throw new Exception("printf invalid width parameter");
					fs.Width = (int)args[pos];
					argPos = pos + 1;
				}
				else {
					try {
						fs.Width = int.Parse(str);
					}
					catch (Exception ex) {
						throw new Exception("printf invalid width parameter", ex);
					}
				}
			}

			fs.PaddingChar = ' ';
			//leading 0 only applies to right justify, for left justify we use ' ' 
			if ((fs.Flags & Flags.LEADING_ZERO_FILL) != 0 && (fs.Flags & Flags.LEFT_JUSTIFY) == 0) {
				fs.PaddingChar = '0';
			}

			//parse the precision field
			str = format.Groups[4].Value;
			if (!string.IsNullOrEmpty(str)) {
				if (str == "*") {
					if (!(pos < args.Length) || !(args[pos] is int)) throw new Exception("printf invalid precision parameter");
					fs.Width = (int)args[pos];
					argPos = pos + 1;
				}
				else {
					try {
						fs.Width = int.Parse(str);
					}
					catch (Exception ex) {
						throw new Exception("printf invalid precision parameter", ex);
					}
				}
			}

			//parse the length field
			str = format.Groups[5].Value;
			if (!string.IsNullOrEmpty(str)) {
				switch (str) {
					case "h": fs.Length = LengthType.SHORT;
						break;
					case "l": fs.Length = LengthType.LONG;
						break;
					default:
						fs.Length = LengthType.NONE;
						break;
				}
			}

			//parse the specifier 
			str = format.Groups[6].Value;
			switch (str[0]) {
				case 'd':
				case 'i':
					fs.Specifier = SpecifierType.SIGNED_INT;               //d, i
					break;
				case 'u':
					fs.Specifier = SpecifierType.UNSIGNED_INT;             //u  
					break;
				case 'o':
					fs.Specifier = SpecifierType.UNSIGNED_OCT;             //o
					break;
				case 'h':
					fs.Specifier = SpecifierType.UNSINGED_HEX;             //h    
					break;
				case 'H':
					fs.Specifier = SpecifierType.UNSIGNED_HEX_UPPER;       //H  
					break;
				case 'f':
					fs.Specifier = SpecifierType.FLOAT;                    //f  
					break;
				case 'F':
					fs.Specifier = SpecifierType.FLOAT_UPPER;             //F
					break;
				case 'e':
					fs.Specifier = SpecifierType.SCIENTIFIC;               //e
					break;
				case 'E':
					fs.Specifier = SpecifierType.SCIENTIFIC_UPPER;         //E
					break;
				case 'g':
					fs.Specifier = SpecifierType.GENERAL;                  //g
					break;
				case 'G':
					fs.Specifier = SpecifierType.GENERAL_UPPER;            //G
					break;
				case 'c':
					fs.Specifier = SpecifierType.CHAR;                     //c
					break;
				case 's':
					fs.Specifier = SpecifierType.STRING;                   //s
					break;
				case '%':
					fs.Specifier = SpecifierType.PERCENT;                  //%
					break;
				case 't':
					fs.Specifier = SpecifierType.DATE_TIME;                //t
					break;
			}
			return true;
		}

		private static bool ParseFormatString(string format, object[] args, out List<string> tokens, out Dictionary<int, FormatSpecification> fmts)
		{
			tokens = null;
			fmts = null;
			MatchCollection matches = _formatRegex.Matches(format);
			if (matches.Count == 0) return false;

			int lastEnd = 0;
			int argPos = 0;
			tokens = new List<string>();
			fmts = new Dictionary<int, FormatSpecification>();

			foreach (Match m in matches) {
				if (lastEnd < m.Index) {
					tokens.Add(format.Substring(lastEnd, m.Index - lastEnd));
				}
				tokens.Add(m.Value);
				//fmtIndexes.Add(tokens.Count - 1);
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
			Dictionary<int, FormatSpecification> fmts;
			int count = 0;

			if (!ParseFormatString(format, args, out tokens, out fmts)) return null;

			//foreach (var f in fmts) 
			//{

			//	//tokens[q] = formatValue(tokens[q], args[count]);
			//	count++;
			//}
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