
using System;
using System.Text;

namespace Starcounter.Query.Execution
{
class MyStringBuilder
{
    StringBuilder stringBuilder;

    internal MyStringBuilder()
    {
        stringBuilder = new StringBuilder();
    }

    internal void Append(String str)
    {
        stringBuilder.Append(str);
    }

    internal void AppendLine(String str)
    {
        stringBuilder.AppendLine(str);
    }

    internal void Append(Int32 tabs, String str)
    {
        for (Int32 i = 0; i < tabs; i++)
        {
            stringBuilder.Append("\t");
        }
        stringBuilder.Append(str);
    }

    internal void AppendLine(Int32 tabs, String str)
    {
        for (Int32 i = 0; i < tabs; i++)
        {
            stringBuilder.Append("\t");
        }
        stringBuilder.AppendLine(str);
    }

    public override String ToString()
    {
        return stringBuilder.ToString();
    }
}
}
