using Starcounter;
using Starcounter.Advanced;
using Ring1;

// Simple class extending Entity
public class Foo1 : Entity {}

// Entity-based class with custom type
public class Foo2 : Entity {
  [Type] public Foo2Type CustomType;
}
[Database] public class Foo2Type {
  [TypeName] public string Name;
}

// A few modelling tests assuring the proposed,
// upcoming Heads model
namespace Ring1 {
	public class Something : Entity {
	  [Type] public SomethingKind Kind;
	}
	public class SomethingKind : Something {}
}

class Program {
	static void Main() {
		var f1Type = Db.TypeOf<Foo1>();
		Assert(f1Type != null, "The Foo1 type should exist");
		
		Db.Transaction(() => {
			var f1 = new Foo1();
			Assert(f1.Type != null, "Foo1.Type should not be NULL");
			Assert(f1.Type.Equals(Db.TypeOf<Foo1>()), "Foo1.Type should be equal to Db.TypeOf<Foo1>");
		});
		
		Db.Transaction(() => {
			var f2 = new Foo2();
			Assert(f2.Type != null, "Foo2.Type should not be NULL");
			Assert(f2.Type.Equals(Db.TypeOf<Foo2>()), "Foo2.Type should be equal to Db.TypeOf<Foo2>");
			Assert(f2.Type.GetType() == typeof(Foo2Type), ".NET type of Foo2.Type should be equal to typeof(Foo2Type)");
			Assert(f2.Type.Equals(f2.CustomType), "Foo2.Type should be equal to Foo2.CustomType");
			Assert(f2.CustomType.GetType() == typeof(Foo2Type), ".NET type of Foo2.CustomType should be equal to typeof(Foo2Type)");
			Assert(Db.TypeOf<Foo2>().GetType() == typeof(Foo2Type), "Db.TypeOf<Foo2>().GetType() should be equal to typeof(Foo2Type)");
			var f2a = Db.SQL<Foo2>("SELECT f2 FROM Foo2 f2 WHERE f2 IS ?", Db.TypeOf<Foo2>()).First;
			Assert(f2a != null, "f2a should not be NULL");
			Assert(f2a.Type.Equals(Db.TypeOf<Foo2>()), "f2a.Type should be equal to Db.TypeOf<Foo2>()");
		});
		
		Db.Transaction(() => {
			var s = new Something();
			Assert(s.Type != null, "s.Type != null");
			Assert(s.Kind != null, "s.Kind != null");
			Assert(Db.TypeOf<Something>() != null, "Db.TypeOf<Something>() != null");
			Assert(Db.TypeOf<Something, SomethingKind>().Name == typeof(Something).FullName, "Db.TypeOf<Something>().Name == typeof(Something).Name");
			Assert(s.Kind.Equals(Db.TypeOf<Something>()), "s.Kind.Equals(Db.TypeOf<Something>())");
			Assert(s.Kind.Name == typeof(Something).FullName, "s.Kind.Name == typeof(Something).Name");
			var s2 = Db.SQL<Something>("SELECT s FROM Something s WHERE s IS ?", Db.TypeOf<Something>()).First;
			Assert(s2 != null, "s2 should not be NULL");
			Assert(s2.Type.Equals(Db.TypeOf<Something>()), "s2.Type should be equal to Db.TypeOf<Something>()");
			Assert(s2.Kind.Equals(Db.TypeOf<Something>()), "s2.Kind should be equal to Db.TypeOf<Something>()");
			Assert(s2.Type.Equals(s2.Kind), "s2.Kind should be equal to Db.TypeOf<Something>()");
		});
		
		var e = TupleHelper.ToTuple(Db.TypeOf<Foo1>());
		Assert(e != null);
		Assert(e.IsType);
		Assert(e.Name == typeof(Foo1).FullName);
		e = TupleHelper.ToTuple(Db.TypeOf<Foo2>());
		Assert(e != null);
		Assert(e.IsType);
		Assert(e.Name == typeof(Foo2).FullName);
		e = TupleHelper.ToTuple(Db.TypeOf<Something>());
		Assert(e != null);
		Assert(e.IsType);
		Assert(e.Name == typeof(Something).FullName);
	}
	
	static void Assert(bool result, string msg = null) {
		msg = msg ?? "Assertion failed";
		ScAssertion.Assert(result, msg);
	}
}