using System;
using System.Collections.Generic;
using System.Text;
using Starcounter;
using Starcounter.Binding;

namespace Starcounter.Poleposition.Util
{
public static class TypeDeleter
{

    public static void DeleteAllOfType<T>()
    {
        using(SqlEnumerator<Object> se = (SqlEnumerator<Object>)Db.SQL(SelectAll<T>.Query).GetEnumerator())
        {
            while (se.MoveNext())
            {
                (se.Current as IObjectProxy).Delete();
            }
        }
    }

    private static class SelectAll<T>
    {
        public static readonly string Query = "SELECT x FROM " + typeof(T).FullName + " x";
    }

}
}
