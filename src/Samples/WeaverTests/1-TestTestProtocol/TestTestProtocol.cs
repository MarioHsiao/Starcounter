
using Starcounter;
using Starcounter.Advanced;
using System;
using System.IO;

class Program {
	
	// Protocol: "WEAVERTEST", inputDirectory, fileName, outputDirectory
	static void Main(string[] args) {
		Console.WriteLine("TestTestProtocol regression test starting");
		Assert(args.Length == 4);
		Assert(args[0] == "WEAVERTEST");
		
		var sourceExe = Path.Combine(args[1], args[2]);
		Assert(Directory.Exists(args[1]));
		Assert(File.Exists(sourceExe));
		Assert(Directory.Exists(args[3]));
	}
	
	static void Assert(bool check, string msg = null) {
		if (!check) {
			msg = msg ?? "TestTestProtocol assertion failed!";
			throw new Exception(msg);
		}
	}
}