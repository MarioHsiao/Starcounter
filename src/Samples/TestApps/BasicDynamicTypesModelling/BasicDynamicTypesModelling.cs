
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
		Assert(ftt.IsType);
		Assert(ftt.Type != null);
		Assert(ftt.Type.IsType);
		Assert(ftt.Type.Type == null);
		
		var bt = Db.TypeOf<Model1.Bar>();
		Assert(bt != null);
		var btt = TupleHelper.ToTuple(bt);
        Assert(btt.Name == typeof(Model1.Bar).FullName);
		Assert(TupleHelper.TupleEquals(btt.Inherits,ftt));
		Assert(btt.IsType);
		Assert(btt.Type != null);
		Assert(btt.Type.IsType);
		Assert(TupleHelper.TupleEquals(btt.Type.Inherits, ftt.Type));
		Assert(btt.Type.Type == null);
	}
	
	static void TestModel2() {
        var ft = Db.TypeOf<Model2.Foo>();
		Assert(ft != null);
        Assert(ft.GetType() == typeof(Model2.FooType));
		var ftt = TupleHelper.ToTuple(ft);
		Assert(ftt.Name == typeof(Model2.Foo).FullName);
		Assert(ftt.IsType);
		Assert(ftt.Type != null);
		Assert(ftt.Type.IsType);
		Assert(ftt.Type.Type == null);
        
        Db.Transact(() => {
           var f = new Model2.Foo();
           var ft2 = TupleHelper.ToTuple(f);
           Assert(TupleHelper.TupleEquals(ft2.Type, ftt));
       });
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