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
        public void Handle(Input.userLink input)
        {
        }

        public void Handle(Input.child.test input)
        {
        }
    }
}
