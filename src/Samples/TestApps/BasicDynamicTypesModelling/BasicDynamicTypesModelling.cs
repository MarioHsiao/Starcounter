using Starcounter;

public class Foo1 : Entity {}

class Program {
	static void Main() {
		var f1 = Db.SQL<Entity>("SELECT e FROM Entity e WHERE e.IsType = ? AND e.Name = ?", true, typeof(Foo1).Name).First;
		ScAssertion.Assert(f1 != null, "The Foo1 type should be found");
		ScAssertion.Assert(f1.Equals(Db.TypeOf<Foo1>()), "The Foo1 type should be equal to Db.TypeOf<Foo>");
	}
}