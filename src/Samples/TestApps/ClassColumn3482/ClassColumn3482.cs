using Starcounter;
using System;

class Program {
	static void Main() {
		Db.Transact(delegate {
			Table t = new Table { TableName = "external table"};
			new Column { ColumnName = "external column", ItsTable = t };
			
		});
        //ScAssertion.Assert(ex != null);
	}
}

namespace AnApp {
	[Database]
	public class Table {
		public string TableName;
	}
	[Database]
	public class Column {
		public string ColumnName;
		public Table ItsTable;
		}
}

[Database]
public class Table {
	public string TableName;
}
[Database]
public class Column {
	public string ColumnName;
	public Table ItsTable;
}
