using Starcounter;

// Simple class extending Entity
public class Foo1 : Entity {}

// Entity-based class with custom type
public class Foo2 : Entity {
  [Type] public Foo2Type CustomType;
}
[Database] public class Foo2Type {
  [TypeName] public string Name;
}

class Program {
	static void Main() {
		var f1 = Db.SQL<Entity>("SELECT e FROM Entity e WHERE e.IsType = ? AND e.Name = ?", true, typeof(Foo1).Name).First;
		ScAssertion.Assert(f1 != null, "The Foo1 type should be found");
		ScAssertion.Assert(f1.Equals(Db.TypeOf<Foo1>()), "The Foo1 type should be equal to Db.TypeOf<Foo1>");
		Db.Transaction(() => {
			f1 = new Foo1();
			ScAssertion.Assert(f1.Type != null, "Foo1.Type should not be NULL");
			ScAssertion.Assert(f1.Type.Equals(Db.TypeOf<Foo1>()), "Foo1.Type should be equal to Db.TypeOf<Foo1>");
		});
		Db.Transaction(() => {
			var f2 = new Foo2();
			ScAssertion.Assert(f2.Type != null, "Foo2.Type should not be NULL");
			ScAssertion.Assert(f2.Type.Equals(Db.TypeOf<Foo2>()), "Foo2.Type should be equal to Db.TypeOf<Foo2>");
			ScAssertion.Assert(f2.Type.GetType() == typeof(Foo2Type), ".NET type of Foo2.Type should be equal to typeof(Foo2Type)");
			ScAssertion.Assert(f2.Type.Equals(f2.CustomType), "Foo2.Type should be equal to Foo2.CustomType");
			ScAssertion.Assert(f2.CustomType.GetType() == typeof(Foo2Type), ".NET type of Foo2.CustomType should be equal to typeof(Foo2Type)");
			ScAssertion.Assert(Db.TypeOf<Foo2>().GetType() == typeof(Foo2Type), "Db.TypeOf<Foo2>().GetType() should be equal to typeof(Foo2Type)");
			var f2a = Db.SQL<Foo2>("SELECT f2 FROM Foo2 f2 WHERE f2 IS ?", Db.TypeOf<Foo2>()).First;
			ScAssertion.Assert(f2a != null, "f2a should not be NULL");
			ScAssertion.Assert(f2a.Type.Equals(Db.TypeOf<Foo2>()), "f2a.Type should be equal to Db.TypeOf<Foo2>()");
		});
	}
}