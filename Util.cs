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

		private static string FORMAT_SPECIFIERS = @"\%(\d*\$)?([\,\#\-\+0 ]*)(\d*|\*)(?:\.(\d+|\*))?([hl])?(\[(.+)\])?([dioxXucsfeEgGpnt%])";
		//%[flags][width][.precision][length][datetime]specifier
		//  2      3      4           5       7        8
		private static Regex _formatRegex = new Regex(FORMAT_SPECIFIERS, RegexOptions.Compiled);

		[FlagsAttribute]
		private enum Flags
		{
			NONE = 0,                      //<the default>
			LEFT_ALIGNED = 1,              //-
			LEADING_ZERO_FILL = 2,         //0
			FORCE_SIGN = 4,			       //+
			INVISIBLE_PLUS_SIGN = 8,       //<space>
			GROUP_THOUSANDS = 16,          //, 
			ALTERNATE = 32                 //#
		}


		private enum SpecifierType
		{
			SIGNED_INT,                    //d, i
			UNSIGNED_INT,                  //u  
			UNSIGNED_OCT,                  //o
			UNSINGED_HEX,                  //h    
			UNSIGNED_HEX_UPPER,            //H  
			FLOAT,                         //f  
			FLOAT_UPPER,                   //F
			SCIENTIFIC,                    //e
			SCIENTIFIC_UPPER,              //E
			GENERAL,                       //g
			GENERAL_UPPER,                 //G
			CHAR,                          //c
			STRING,                        //s
			PERCENT,                       //%
			DATE_TIME,                     //t
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
			//public char PaddingChar { get; set; }
			public string FomratString { get; set; }
			public SpecifierType Specifier { get; set; }

			public string Format(object o)
			{
				switch (this.Specifier) {
					case SpecifierType.SIGNED_INT:
						return FormatNumber("d", o);
					case SpecifierType.UNSIGNED_INT:
						return FormatNumber("d", o);
					case SpecifierType.UNSIGNED_OCT:
						return FormatNumber("o", o);
					case SpecifierType.UNSINGED_HEX:
						return FormatNumber("x", o);
					case SpecifierType.UNSIGNED_HEX_UPPER:
						return FormatNumber("X", o);
					case SpecifierType.FLOAT:
						return FormatNumber("f", o);
					case SpecifierType.FLOAT_UPPER:
						return FormatNumber("F", o);
					case SpecifierType.SCIENTIFIC:
						return FormatNumber("e", o);
					case SpecifierType.SCIENTIFIC_UPPER:
						return FormatNumber("E", o);
					case SpecifierType.GENERAL:
						return FormatNumber("g", o);
					case SpecifierType.GENERAL_UPPER:
						return formatValue("G", o);
					case SpecifierType.CHAR:
						if (o is int) {
							return FormatString(((char)((int)o)).ToString());
						}
						return FormatString(o.ToString());
					case SpecifierType.STRING:
						return FormatString(o.ToString());
					case SpecifierType.PERCENT:
						return "%";
					case SpecifierType.DATE_TIME:
						if (o is TimeSpan) {
							TimeSpan d = (TimeSpan)o;
							return FormatString(d.ToString(this.FomratString));
						}
						if (o is DateTime) {
							DateTime d = (DateTime)o;
							return FormatString(d.ToString(this.FomratString));
						}
						return o.ToString();
				}
				return null;
			}

			private string FormatNumber(string specifier, object Value)
			{
				if (specifier == "d" || specifier == "f" || specifier == "u") {
					if ((this.Flags & Util.Flags.GROUP_THOUSANDS) != 0) specifier = "n";
				}
				string str = "{0:" + specifier + this.Precision + "}";
				str = string.Format(str, Value);
				if ((this.Flags & Util.Flags.FORCE_SIGN) != 0) {
					if (str[0] != '-') str = "+" + str;
				}
				if (str.Length < this.Width) {
					if ((this.Flags & Util.Flags.LEFT_ALIGNED) != 0) return str = str.PadRight(this.Width, ' ');
					//right aligned?
					if (((this.Flags & Util.Flags.LEADING_ZERO_FILL) != 0) && (str[0] == '-' || str[0] == '+')) {
						return str[0] + str.Substring(1).PadLeft(this.Width - 1, '0');
					}
					else {
						return str.PadLeft(this.Width, ((this.Flags & Util.Flags.LEADING_ZERO_FILL) != 0) ? '0' : ' ');
					}
				}
				return str;
			}

			private string FormatString(string value)
			{
				if (this.Width > value.Length) {
					if ((this.Flags & Util.Flags.LEFT_ALIGNED) != 0) return value.PadRight(this.Width);
					return value.PadLeft(this.Width);
				}
				return value;
			}
		}



		private static FormatSpecification ParseFormatSpec(Match match)
		{
			FormatSpecification fs = new FormatSpecification();

			string str = match.Groups[2].Value;

			if (str.Contains("-")) fs.Flags |= Flags.LEFT_ALIGNED;
			if (str.Contains("+")) fs.Flags |= Flags.FORCE_SIGN;
			if (str.Contains("#")) fs.Flags |= Flags.ALTERNATE;
			if (str.Contains("0")) fs.Flags |= Flags.LEADING_ZERO_FILL;
			if (str.Contains(",")) fs.Flags |= Flags.GROUP_THOUSANDS;

			if (str.Contains(" ")) {				
				if ((fs.Flags & Flags.FORCE_SIGN) == 0) {
					//force + overrides the invisible zero sign, set INVISIBLE_PLUS_SIGN
					//only if FORCE_SIGN is not set
					fs.Flags = Flags.INVISIBLE_PLUS_SIGN;
				}
			}

			//parse the width field
			str = match.Groups[3].Value;
			if (!string.IsNullOrEmpty(str)) {			
				if (str == "*") {
					//make the value as int.MinValue which will be resolved later by reading
					//the preceding argument
					fs.Width = int.MinValue;
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

			//fs.PaddingChar = ' ';
			////leading 0 only applies to right justify, for left justify we use ' ' 
			//if ((fs.Flags & Flags.LEADING_ZERO_FILL) != 0 && (fs.Flags & Flags.LEFT_ALIGNED) == 0) {
			//	fs.PaddingChar = '0';
			//}

			//parse the precision field
			str = match.Groups[4].Value;
			if (!string.IsNullOrEmpty(str)) {
				if (str == "*") {
					//make the value as int.MinValue which will be resolved later by reading
					//the preceding argument
					fs.Precision = int.MinValue;
				}
				else {
					try {
						fs.Precision = int.Parse(str);
					}
					catch (Exception ex) {
						throw new Exception("printf invalid precision parameter", ex);
					}
				}
			}

			//parse the length field
			str = match.Groups[5].Value;
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

			//the format string
			fs.FomratString = match.Groups[7].Value;

			//parse the specifier 
			str = match.Groups[8].Value;
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
				case 'x':
					fs.Specifier = SpecifierType.UNSINGED_HEX;             //x    
					break;
				case 'X':
					fs.Specifier = SpecifierType.UNSIGNED_HEX_UPPER;       //X 
					break;
				case 'f':
					fs.Specifier = SpecifierType.FLOAT;                    //f  
					break;
				case 'F':
					fs.Specifier = SpecifierType.FLOAT_UPPER;              //F
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
				case 't':
					fs.Specifier = SpecifierType.DATE_TIME;                //t
					break;
				case '%':
					fs.Specifier = SpecifierType.PERCENT;                  //%
					break;
			}

			return fs;
		}


		private static bool ParseFormatString(string format, object[] args, out List<string> tokens)
		{
			tokens = null;
			MatchCollection matches = _formatRegex.Matches(format);
			FormatSpecification fs;
			if (matches.Count == 0) return false;

			int lastEnd = 0;
			int argPos = 0;
			tokens = new List<string>();

			foreach (Match m in matches) {
				if (lastEnd < m.Index) {
					tokens.Add(format.Substring(lastEnd, m.Index - lastEnd));
				}
				fs = ParseFormatSpec(m);
				if (fs.Width == int.MinValue) {
					if (!(argPos < args.Length) || !(args[argPos] is int)) throw new Exception("printf width parameter was not provided");
					fs.Width = (int)args[argPos];
					argPos = argPos + 1;
				}
				if (fs.Precision == int.MinValue) {
					if (!(argPos < args.Length) || !(args[argPos] is int)) throw new Exception("printf precision parameter was not provided");
					fs.Precision = (int)args[argPos];
					argPos = argPos + 1;
				}
				tokens.Add(fs.Format(args[argPos]));
				argPos++;
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
			if (!ParseFormatString(format, args, out tokens)) return null;
			return tokens;
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
			if (pieces == null || pieces.Count == 0) return format;
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