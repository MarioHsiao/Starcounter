// ***********************************************************************
// <copyright file="Master.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using NUnit.Framework;
using Starcounter;
using Starcounter.Internal.ExeModule;
using Starcounter.Templates;
using System;

/// <summary>
/// Class Master
/// </summary>
partial class Master : App {

    /// <summary>
    /// Initializes static members of the <see cref="Master" /> class.
    /// </summary>
    static Master() {
            AppExeModule.IsRunningTests = true;
    }

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


        x.ProcessInput(m,"Hej hopp");


    }
}
