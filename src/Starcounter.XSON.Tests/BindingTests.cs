
using NUnit.Framework;
using Starcounter.Templates;
namespace Starcounter.Internal.XSON.Tests {

    public class BindingTests {

        [Test]
        public static void TestSimpleBinding() {
            var p = new Person();
            p.FirstName = "Joachim";
            p.LastName = "Wester";

            dynamic j = new Json();
            TJson t = new TJson();
            var prop = t.Add<TString>("FirstName");
            prop.Bind = "FirstName";
            prop.Bound = Bound.Yes;
            j.Template = t;
            j.Data = p;

            Assert.AreEqual("Joachim", p.FirstName);
            Assert.AreEqual("Joachim", j.FirstName);

            j.FirstName = "Douglas";
            Assert.AreEqual("Douglas", j.FirstName);
            Assert.AreEqual("Douglas", p.FirstName);
        }
    }
}
