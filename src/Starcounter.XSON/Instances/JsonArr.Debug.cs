using System.Text;

namespace Starcounter {
    partial class Arr {

        internal override void WriteToDebugString(StringBuilder sb, int indentation) {
            _WriteDebugProperty(sb);

            sb.Append("[");
            indentation += 3;
            int t = 0;
            foreach (var e in QuickAndDirtyArray) {
                if (t > 0) {
                    sb.AppendLine(",");
                    sb.Append(' ', indentation);
                }
                e.WriteToDebugString(sb, indentation);
                t++;
            }
            indentation -= 3;
            sb.AppendLine();
            sb.Append(' ', indentation);
            sb.Append("]");
        }
    }
}
