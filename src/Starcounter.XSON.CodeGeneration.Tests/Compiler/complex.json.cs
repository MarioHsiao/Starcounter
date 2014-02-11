﻿// ***********************************************************************
// <copyright file="MySampleJson.json.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using MySampleNamespace.Something;
using SomeOtherNamespace;

public class ClassWithoutNamespace : Page {
    static void Main() {
        using (string s = "") {

        }

        Handle.POST("/init-demo-data", () => {
            return 201;
        });
    }
}

namespace MySampleNamespace
{
    namespace WrongNamespace
    {
        /// <summary>
        /// Class WrongClass
        /// </summary>
        [Test]
        public class WrongClass
        {
            public string Apa;

            public void GetApa() {
                return Apa;
            }
        }
    }

    /// <summary>
    /// Class MySampleJson
    /// </summary>
	[Complex_json]
    partial class Complex : MyBaseJsonClass, ISomeInterface, IBound<Order>
    {
        /// <summary>
        /// Handles the specified input.
        /// </summary>
        /// <param name="input">The input.</param>
        public void Handle(Input.userLink input)
        {
        }

        /// <summary>
        /// Handles the specified input.
        /// </summary>
        /// <param name="input">The input.</param>
        public void Handle(Input.child.test input)
        {
        }

#region Test of several inputhandler registration and sortorder.
        /// <summary>
        /// Handles the specified input.
        /// </summary>
        /// <param name="input">The input.</param>
        public void Handle(Input.ActivePage.SubPage1.SubPage2.SubPage3.StringValue input)
        {
        }

        /// <summary>
        /// Class SubPage3Impl
        /// </summary>
        [Another(Fake=true), Test]
        [json.ActivePage.SubPage1.SubPage2.SubPage3]
        [SomeOther]
        public partial class SubPage3Impl : Json, IFoo, IFoo3, IBound<OrderItem>
        {
            [json.Blabla.bla]
            public partial class SubPage3Sub1 : Json {
            }

            /// <summary>
            /// Handles the specified input.
            /// </summary>
            /// <param name="input">The input.</param>
            public void Handle(Input.ActivePage.SubPage1.SubPage2.SubPage3.StringValue input)
            {
            }
        }

        /// <summary>
        /// Class ActivePageImpl
        /// </summary>
        [json.ActivePage]
        public partial class ActivePageImpl : Json, IFoo
        {
            /// <summary>
            /// Handles the specified input.
            /// </summary>
            /// <param name="input">The input.</param>
            public void Handle(Input.ActivePage.SubPage1.SubPage2.SubPage3.StringValue input)
            {
            }
        }

        /// <summary>
        /// Class SubPage2Impl
        /// </summary>
        [json.ActivePage.SubPage1.SubPage2]
        public partial class SubPage2Impl : Json
        {
            /// <summary>
            /// Handles the specified input.
            /// </summary>
            /// <param name="input">The input.</param>
            public void Handle(Input.ActivePage.SubPage1.SubPage2.SubPage3.StringValue input)
            {
            }
        }
   
        /// <summary>
        /// Class SubPage1Impl
        /// </summary>
        [json.ActivePage.SubPage1]
        public partial class SubPage1Impl : Json
        {
            /// <summary>
            /// Handles the specified input.
            /// </summary>
            /// <param name="input">The input.</param>
            public void Handle(Input.ActivePage.SubPage1.SubPage2.SubPage3.StringValue input)
            {
            }
        }
#endregion
    }
}
