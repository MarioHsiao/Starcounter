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

        public void Handle(Input.items.itemname input)
        {
        }

        [Json.MySampleApp.items]
        partial class ItemsImplApp
        {
            public void Handle(Input.items.itemname input)
            {

            }
        }
    }
}
