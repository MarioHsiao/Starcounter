using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Poleposition.Framework
{
[global::System.AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class LapAttribute : Attribute
{
    // See the attribute guidelines at
    //  http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconusingattributeclasses.asp
    readonly string name;

    // This is a positional argument.
    public LapAttribute(string name)
    {
        this.name = name;
    }
    public string Name
    {
        get
        {
            return this.name;
        }
    }
}

}
