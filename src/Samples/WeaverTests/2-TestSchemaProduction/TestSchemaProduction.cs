
using Starcounter;
using Starcounter.Advanced;
using System;
using System.IO;
using Sc.Server.Weaver.Schema;
using Database = Starcounter.DatabaseAttribute;

[Database] public class Foo {}

class Program {
	
	// Protocol: "WEAVERTEST", inputDirectory, fileName, outputDirectory
	static void Main(string[] args) {
		Console.WriteLine("TestSchemaProduction regression test starting");
		var schema = DatabaseSchema.DeserializeFrom(new DirectoryInfo(args[3]).GetFiles("*.schema"));
		Assert(schema != null);
		Assert(schema.FindDatabaseClass("Foo") != null);
	}
	
	static void Assert(bool check, string msg = null) {
		if (!check) {
			msg = msg ?? "TestTestProtocol assertion failed!";
			throw new Exception(msg);
		}
	}
}