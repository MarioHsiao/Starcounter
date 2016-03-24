// ***********************************************************************
// <copyright file="MySampleApp.json.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

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

    partial class MySampleApp : Json
    {
        public void Handle(Input.userLink input)
        {
        }

        [MySampleApp_json.ActivePage.SubPage1.SubPage2.SubPage3]
        public partial class SubPage3Impl : Json<Order>, IFoo, IFoo3
        {
            public void Handle(Input.StringValue input)
            {
            }
        }

        [MySampleApp_json.ActivePage]
        public partial class ActivePageImpl : Json, IFoo
        {
            
        }

        [MySampleApp_json.ActivePage.SubPage1.SubPage2]
        public partial class SubPage2Impl : Json
        {
            
        }

        [MySampleApp_json.ActivePage.SubPage1]
        public partial class SubPage1Impl : Json
        {
        }
    }
}
