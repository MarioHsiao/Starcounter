using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Starcounter;
using Starcounter.Binding;
using Starcounter.Query.Execution;

namespace Sc.Query.RawParserAnalyzer
{
    internal partial class RawParserAnalyzer
    {
        internal unsafe TypeBinding GetTypeBindingFor(RangeVar* extent)
        {
            Debug.Assert(extent->relname != null);
            String shortname = new String(extent->relname);
            TypeBinding theType = TypeRepository.GetTypeBinding(shortname);
            if (theType != null)
                return theType;
            int res = TypeRepository.TryGetTypeBindingByShortName(shortname, out theType);
            if (res == 1)
                return theType;
            throw ErrorCode.ToException(Error.SCERRSQLUNKNOWNNAME, LocationMessageForError((Node*)extent, shortname));
        }

        internal bool CompareTo(IExecutionEnumerator otherOptimizedPlan)
        {
            String thisOptimizedPlanStr = Regex.Replace(this.OptimizedPlan.ToString(), "\\s", "");
            String otherOptimizedPlanStr = Regex.Replace(otherOptimizedPlan.ToString(), "\\s", "");
            return thisOptimizedPlanStr.Equals(otherOptimizedPlanStr);
            //return this.OptimizedPlan.ToString().Equals(otherOptimizedPlan.ToString());
        }
    }
}
