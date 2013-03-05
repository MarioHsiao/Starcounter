using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.Poleposition.Util
{
public static class TypeDeleter
{

    public static void DeleteAllOfType<T>()
    where T : Entity
    {
        using(SqlEnumerator<Object> se = (SqlEnumerator<Object>)Db.SQL(SelectAll<T>.Query).GetEnumerator())
        {
            while (se.MoveNext())
            {
                (se.Current as Entity).Delete();
            }
        }
    }

    private static class SelectAll<T> where T : Entity
    {
        public static readonly string Query = "SELECT x FROM " + typeof(T).FullName + " x";
    }

}
}
