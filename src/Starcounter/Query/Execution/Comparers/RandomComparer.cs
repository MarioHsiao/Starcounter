
using Starcounter;
using Starcounter.Query.Sql;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Starcounter.Query.Execution
{
/// <summary>
/// Class that holds information about a random comparer which is a comparer that
/// gives a random result when comparing composite objects.
/// </summary>
internal class RandomComparer : IQueryComparer
{
    Random random;

    internal RandomComparer()
    {
        random = new Random();
    }

    public Int32 Compare(CompositeObject obj1, CompositeObject obj2)
    {
        if (obj1.Random == -1)
        {
            obj1.Random = random.Next();
        }
        if (obj2.Random == -1)
        {
            obj2.Random = random.Next();
        }
        return obj1.Random.CompareTo(obj2.Random);
    }

    public IQueryComparer Clone(VariableArray varArray)
    {
        return new RandomComparer();
    }


    public void BuildString(MyStringBuilder stringBuilder, Int32 tabs)
    {
        stringBuilder.AppendLine(tabs, "RandomComparer()");
    }

    /// <summary>
    /// Generates compilable code representation of this data structure.
    /// </summary>
    public void GenerateCompilableCode(CodeGenStringGenerator stringGen)
    {
        stringGen.AppendLine(CodeGenStringGenerator.CODE_SECTION_TYPE.FUNCTIONS, "RandomComparer");
    }
}
}
