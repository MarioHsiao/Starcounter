
using Sc.Server.Binding;
using Starcounter;
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
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                Type fieldType = field.FieldType;
                if (typeof(Entity).IsAssignableFrom(fieldType))
                {
                    string typeName = fieldType.FullName;
                    TypeBinding tb = BindingRegistry.GetTypeBinding(fieldType.FullName);
                    if (tb != null)
                    {
                        field = type.GetField(typeName + "__typeAddress", BindingFlags.Static | BindingFlags.NonPublic);
                        field.SetValue(null, tb.TableDef.DefinitionAddr);
                        field = type.GetField(typeName + "__typeBinding", BindingFlags.Static | BindingFlags.NonPublic);
                        field.SetValue(null, tb);
                    }
                    else
                    {
//                        throw sccoreerr.TranslateErrorCode(scerrres.SCERRUNSPECIFIED);
                    }
                }
            }
        }
    }
}
