
using Starcounter;
using Starcounter.Advanced;

namespace Model1 {
	[Database] public class Foo {}
	[Database] public class Bar : Foo {}
}

namespace Model2 {
	[Database] public class Foo { [Type] public FooType Type; }
	[Database] public class FooType {}
}

namespace Model3 {
	[Database] public class Foo : Entity {}
}

class Program {

	static void TestModel1() {
		var ft = Db.TypeOf<Model1.Foo>();
		Assert(ft != null);
		var ftt = TupleHelper.ToTuple(ft);
		Assert(ftt.Name == typeof(Model1.Foo).FullName);
		
		var bt = Db.TypeOf<Model1.Bar>();
		Assert(bt != null);
		var btt = TupleHelper.ToTuple(bt);
        Assert(btt.Name == typeof(Model1.Bar).FullName);
		Assert(TupleHelper.TupleEquals(btt.Inherits,ftt));

        Db.Transact(() => {
            var f = new Model1.Foo();
            var ft2 = TupleHelper.ToTuple(f);
            Assert(TupleHelper.TupleEquals(ft2.Type, ftt));
        });
	}
	
	static void TestModel2() {
	}
	
	static void TestModel3() {
	}
	
	static void Main() {
		TestModel1();
		TestModel2();
		TestModel3();
	}
	
	static void Assert(bool result, string msg = null) {
		msg = msg ?? "Assertion failed";
		ScAssertion.Assert(result, msg);
	}
}