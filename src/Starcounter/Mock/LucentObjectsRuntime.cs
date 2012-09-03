
using Sc.Server.Binding;
using Starcounter;
using Starcounter.Binding;
using Starcounter.Internal;
using System;
using System.Reflection;

namespace Starcounter.LucentObjects
{
    
    public static class LucentObjectsRuntime
    {

        public static void InitializeClientAssembly(Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.NonPublic);
            for (int fi = 0; fi < fields.Length; fi++)
            {
                FieldInfo field = fields[fi];
                Type fieldType = field.FieldType;
                if (typeof(Entity).IsAssignableFrom(fieldType))
                {
                    string typeName = fieldType.FullName;
                    TypeDef typeDef = Bindings.GetTypeDef(typeName);
                    if (typeDef != null)
                    {
                        TypeBinding tb = Bindings.GetTypeBinding(fieldType.FullName);

                        field = type.GetField(typeName + "__typeAddress", BindingFlags.Static | BindingFlags.NonPublic);
                        field.SetValue(null, tb.DefHandle);
                        field = type.GetField(typeName + "__typeBinding", BindingFlags.Static | BindingFlags.NonPublic);
                        field.SetValue(null, tb);

                        ColumnDef[] columns = tb.TypeDef.TableDef.ColumnDefs;
                        for (int ci = 0; ci < columns.Length; ci++)
                        {
                            field = fieldType.GetField("<>0" + columns[ci].Name + "000", BindingFlags.Static | BindingFlags.NonPublic);
                            field.SetValue(null, ci);
                        }
                    }
                    else
                    {
//                        throw sccoreerr.TranslateErrorCode(Error.SCERRUNSPECIFIED);
                    }
                }
            }
        }
    }
}
