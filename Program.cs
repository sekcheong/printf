using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.IO;
using Axiom;

namespace printf
{
	

	class Program
	{
		static void Main(string[] args)
		{
			Util.printf("%+-007.5hf Hello World\n Name: %,s Number%0d abcd %t[hh:mm] 1234", "John", 100, "A", DateTime.Now);
		}
	}
}
