using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Starcounter;
using Starcounter.Binding;
using Starcounter.Query.Execution;

namespace Starcounter.Query.RawParserAnalyzer
{
    internal partial class ParserAnalyzer
    {
        // I should investigate the exception first, since it might be not related
        internal unsafe String GetFullName(RangeVar* extent) {
            Debug.Assert(extent->path != null);
            Debug.Assert(extent->relname == null);
            ListCell* curCell = extent->path->head;
            Debug.Assert(curCell != null);
            Debug.Assert(((Node *)curCell->data.ptr_value)->type == NodeTag.T_ColumnRef, "Expected T_ColumnRef, but got " +
                ((Node*)curCell->data.ptr_value)->type.ToString());
            String name = new String(((ColumnRef*)curCell->data.ptr_value)->name);
            curCell = curCell->next;
            while (curCell != null) {
                name += '.';
                name += new String(((ColumnRef*)curCell->data.ptr_value)->name);
                curCell = curCell->next;
            }
            return name;
        }

        internal unsafe TypeBinding GetTypeBindingFor(RangeVar* extent)
        {
            //Debug.Assert(extent->relname != null);
            String relName = GetFullName(extent);
            TypeBinding theType = null;
            try {
                theType = Bindings.GetTypeBindingInsensitive(relName);
            } catch (DbException ex) {
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
