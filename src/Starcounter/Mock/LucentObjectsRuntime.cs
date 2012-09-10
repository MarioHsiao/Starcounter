
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

        public static void InitializeClientAssembly(Type clientAssemblyTypeInitializer, Type type)
        {
            string typeName = type.FullName;
            TypeDef typeDef = Bindings.GetTypeDef(typeName);
            if (typeDef != null)
            {
                TypeBinding tb = Bindings.GetTypeBinding(type.FullName);

                FieldInfo field;
                field = clientAssemblyTypeInitializer.GetField(typeName + "__typeAddress", BindingFlags.Static | BindingFlags.NonPublic);
                field.SetValue(null, tb.DefHandle);
                field = clientAssemblyTypeInitializer.GetField(typeName + "__typeBinding", BindingFlags.Static | BindingFlags.NonPublic);
                field.SetValue(null, tb);

                ColumnDef[] columns = tb.TypeDef.TableDef.ColumnDefs;
                for (int ci = 0; ci < columns.Length; ci++)
                {
                    ColumnDef column = columns[ci];

                    field = type.GetField("<>0" + column.Name + "000", BindingFlags.Static | BindingFlags.NonPublic);
                    if (!column.IsInherited)
                    {
                        try
                        {
                            field.SetValue(null, ci);
                        }
                        catch (NullReferenceException)
                        {
                            throw sccoreerr.TranslateErrorCode(Error.SCERRSCHEMACODEMISMATCH);
                        }
                    }
                    else if (field != null)
                    {
                        // If inherited column there should be no field to
                        // store the column index.

                        throw sccoreerr.TranslateErrorCode(Error.SCERRSCHEMACODEMISMATCH);
                    }
                }
            }
            else
            {
                throw sccoreerr.TranslateErrorCode(Error.SCERRSCHEMACODEMISMATCH);
            }
        }
    }
}
