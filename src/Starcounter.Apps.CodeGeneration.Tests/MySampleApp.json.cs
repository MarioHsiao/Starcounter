using System;

namespace MySampleNamespace
{
    namespace WrongNamespace
    {
        [Test]
        public class WrongClass
        {
        }
    }

    partial class MySampleApp
    {
    }

    public class Test : Attribute {
    }
}
