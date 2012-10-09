using NUnit.Framework;
using Starcounter;
using Starcounter.Internal.ExeModule;
using Starcounter.Templates;
using System;

partial class Master : App {

    static Master() {
            AppExeModule.IsRunningTests = true;
    }

    void Handle(Input.Test test) {
        test.Cancel();
    }

    [Test]
    public static void TestInput() {



        var m = new Master();
        StringProperty x = m.Template.Test;


        x.ProcessInput(m,"Hej hopp");


    }
}
