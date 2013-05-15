// ***********************************************************************
// <copyright file="Master.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using NUnit.Framework;
using Starcounter;
using Starcounter.Templates;
using System;

/// <summary>
/// Class Master
/// </summary>
partial class Master : Json {

    /// <summary>
    /// Handles the specified test.
    /// </summary>
    /// <param name="test">The test.</param>
    void Handle(Input.Test test) {
        test.Cancel();
    }

    /// <summary>
    /// Tests the input.
    /// </summary>
    [Test]
    public static void TestInput() {



        var m = new Master();
        TString x = m.Template.Test;


        m.ProcessInput<string>(x,"Hej hopp");


    }
}
