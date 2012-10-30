/* This file contains methods, which are used for and by tests.
 */

using System;
using Starcounter.Binding;

namespace Sc.Query.RawParserAnalyzer
{
    internal partial class RawParserAnalyzer
    {
        internal static String GetTypeFor(String typeName)
        {
            TypeBinding theType = TypeRepository.GetTypeBinding(typeName);
            if (theType != null)
                return theType.Name;
            int res = TypeRepository.TryGetTypeBindingByShortName(typeName, out theType);
            if (res == 1)
                return theType.Name;
            else
                return null;
        }
    }
}
