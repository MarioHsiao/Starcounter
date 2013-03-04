using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Starcounter.Poleposition.Util
{
public static class Attributes
{
    public static T Find<T>(ICustomAttributeProvider obj, bool inherit)
    where T : Attribute
    {
        foreach (Attribute attr in obj.GetCustomAttributes(inherit))
        {
            if (attr is T)
            {
                return (T)attr;
            }
        }
        return null;
    }
}
}
