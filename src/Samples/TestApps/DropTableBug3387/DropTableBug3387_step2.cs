using Starcounter;
using System;

class Program {
    static void Main() {
        Console.WriteLine("{0} starting...", Application.Current);
		Db.SQL("DROP TABLE Foo");
    }
}
