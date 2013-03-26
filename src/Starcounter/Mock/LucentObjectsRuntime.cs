// ***********************************************************************
// <copyright file="LucentObjectsRuntime.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Binding;
using Starcounter.Internal;
using System;
using System.Reflection;

namespace Starcounter.LucentObjects
{
    /*
     * This class should and will shortly be replaced by similar functionality in the
     * new hosting infrastruture as found in the Starcounter.Hosting namespace, defined
     * in this same assembly.
     */

    /// <summary>
    /// Class LucentObjectsRuntime
    /// </summary>
    public static class LucentObjectsRuntime
    {

        /// <summary>
        /// Initializes the client assembly.
        /// </summary>
        /// <param name="clientAssemblyTypeInitializer">The client assembly type initializer.</param>
        /// <param name="type">The type.</param>
        public static void InitializeClientAssembly(Type clientAssemblyTypeInitializer, Type type)
        {
            string typeName = type.FullName;

            TypeBinding tb = Bindings.GetTypeBinding(type.FullName);

            FieldInfo field;
            field = clientAssemblyTypeInitializer.GetField(typeName + "__typeTableId", BindingFlags.Static | BindingFlags.NonPublic);
            field.SetValue(null, tb.TableId);
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
                        throw ErrorCode.ToException(Error.SCERRSCHEMACODEMISMATCH);
                    }
                }
                else if (field != null)
                {
                    // If inherited column there should be no field to
                    // store the column index.

                    throw ErrorCode.ToException(Error.SCERRSCHEMACODEMISMATCH);
                }
            }
        }
    }
}
