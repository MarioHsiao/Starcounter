using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Poleposition.Framework
{
[global::System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
sealed class DriverAttribute : Attribute
{
    // See the attribute guidelines at
    //  http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconusingattributeclasses.asp
    readonly string driverName;

    public DriverAttribute(string driverName)
    {
        this.driverName = driverName;
    }
    public string DriverName
    {
        get
        {
            return this.driverName;
        }
    }
}

}
