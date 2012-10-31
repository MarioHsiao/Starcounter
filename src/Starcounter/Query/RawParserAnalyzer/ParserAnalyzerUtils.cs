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
        // I should investigate the exception first, since it might be not related
        internal unsafe String GetFullName(RangeVar* extent) {
            String name = null;
            if (extent->namespaces == null)
                return new String(extent->relname);
            ListCell* curCell = extent->namespaces->head;
            while (curCell != null) {
                name += new String(((Value*)curCell->data.ptr_value)->val.str);
                name += '.';
                curCell = curCell->next;
            }
            return name + new String(extent->relname);
        }

        internal unsafe TypeBinding GetTypeBindingFor(RangeVar* extent)
        {
            Debug.Assert(extent->relname != null);
            String relName = GetFullName(extent);
            TypeBinding theType = null;
            try {
                theType = Bindings.GetTypeBinding(relName);
            } catch (Exception ex) {
                throw ErrorCode.ToException(Error.SCERRSQLUNKNOWNNAME, ex, LocationMessageForError((Node*)extent, relName));
            }
            if (theType != null)
                return theType;
            //int res = TypeRepository.TryGetTypeBindingByShortName(shortname, out theType);
            //if (res == 1)
            //    return theType;
            throw ErrorCode.ToException(Error.SCERRSQLUNKNOWNNAME, LocationMessageForError((Node*)extent, relName));
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
