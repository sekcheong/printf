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
			//Util.printf("Integer: %X\n", 102003);
			Util.printf("Double: %20.*e\n", 2, -108880.12945);
			//Util.printf("Date  : %*[MM:dd:yyyy]t\n", 20, DateTime.Now);
			Console.WriteLine();
		}
	}
}
