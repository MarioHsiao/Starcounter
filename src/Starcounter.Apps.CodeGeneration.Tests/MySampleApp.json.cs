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

#region Test of several inputhandler registration and sortorder.
        public void Handle(Input.ActivePage.SubPage1.SubPage2.SubPage3.StringValue input)
        {
        }

        [Json.ActivePage.SubPage1.SubPage2.SubPage3]
        public partial class SubPage3Impl : App
        {
            public void Handle(Input.ActivePage.SubPage1.SubPage2.SubPage3.StringValue input)
            {
            }
        }

        [Json.ActivePage]
        public partial class ActivePageImpl : App
        {
            public void Handle(Input.ActivePage.SubPage1.SubPage2.SubPage3.StringValue input)
            {
            }
        }

        [Json.ActivePage.SubPage1.SubPage2]
        public partial class SubPage2Impl : App
        {
            public void Handle(Input.ActivePage.SubPage1.SubPage2.SubPage3.StringValue input)
            {
            }
        }

        [Json.ActivePage.SubPage1]
        public partial class SubPage1Impl : App
        {
            public void Handle(Input.ActivePage.SubPage1.SubPage2.SubPage3.StringValue input)
            {
            }
        }
#endregion
    }
}
