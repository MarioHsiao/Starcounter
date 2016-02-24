using Starcounter;
using System;

[Database] public class Foo {
}

class Program {
    static void Main() {
        Db.Transact(() => {
			new Foo();
		});
    }
}
