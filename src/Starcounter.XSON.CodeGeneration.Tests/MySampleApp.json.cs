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
        /// <summary>
        /// Class WrongClass
        /// </summary>
        [Test]
        public class WrongClass
        {
        }
    }

    /// <summary>
    /// Class MySampleApp
    /// </summary>
    partial class MySampleApp : Json
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
        [json.ActivePage.SubPage1.SubPage2.SubPage3]
        public partial class SubPage3Impl : Json<Order>, IFoo, IFoo3
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
