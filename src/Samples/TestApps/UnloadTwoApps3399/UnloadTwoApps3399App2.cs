using Starcounter;
using System;

[Database]
public class Bar {
}

class Program {
    static void Main() {
		Db.Transact(() => {
			new Bar();
		});
    }
}
