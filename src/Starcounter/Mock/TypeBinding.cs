
using Starcounter;
using Starcounter.Query.Execution;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Sc.Server.Binding
{

    public class TypeBinding : TypeOrExtensionBinding
    {

        public Type Type;
        public TypeBinding Base;

        public TypeBinding(TableDef tableDef) : base(tableDef) { }

        public ExtensionBinding GetExtensionBinding(string name)
        {
            throw new System.NotImplementedException();
        }

        public System.Collections.Generic.IEnumerator<ExtensionBinding> GetAllExtensionBindings()
        {
            return new System.Collections.Generic.List<ExtensionBinding>().GetEnumerator();
        }

        public DbObject NewInstanceUninit()
        {
            ConstructorInfo ctor = Type.GetConstructor(new Type[] { typeof(Sc.Server.Internal.Uninitialized) });
            DbObject obj = (DbObject)ctor.Invoke(new object[] { null });
            return obj;
        }

        public DbObject NewInstance(ulong addr, ulong oid)
        {
            DbObject obj = NewInstanceUninit();
            obj.Attach(addr, oid, this);
            return obj;
        }

        public bool SubTypeOf(TypeBinding tb)
        {
            throw new System.NotImplementedException();
        }
    }

    public class ExtensionBinding : TypeOrExtensionBinding
    {

        public int Index;

        public ExtensionBinding(TableDef tableDef) : base(tableDef) { }
    }
    
    public class TypeOrExtensionBinding : ITypeBinding
    {

        public readonly TableDef TableDef;

        public TypeOrExtensionBinding(TableDef tableDef)
        {
            TableDef = tableDef;
        }

        public ulong DefHandle { get { return TableDef.DefinitionAddr; } }

        internal IndexInfo[] GetAllIndexInfos()
        {
            UInt32 ec;
            UInt32 ic;
            sccoredb.SC_INDEX_INFO[] iis;
            List<IndexInfo> iil;
            Int32 i;
            String name;
            Int16 attributeCount;
            UInt16 tempSortMask;
            SortOrder[] sortOrderings;
            PropertyBinding[] propertyBindings;
            Boolean nonBelongingPropertyBinding;

            unsafe
            {
                ec = sccoredb.SCSchemaGetIndexes(
                    DefHandle,
                    &ic,
                    null
                );

                if (ec != 0)
                {
                    throw ErrorCode.ToException(ec);
                }

                if (ic == 0)
                {
                    return new IndexInfo[0];
                }

                iis = new sccoredb.SC_INDEX_INFO[ic];

                fixed (sccoredb.SC_INDEX_INFO* p = &(iis[0]))
                {
                    ec = sccoredb.SCSchemaGetIndexes(
                        DefHandle,
                        &ic,
                        p
                    );
                }

                if (ec != 0)
                {
                    throw ErrorCode.ToException(ec);
                }

                iil = new List<IndexInfo>((Int32)ic);

                for (i = 0; i < ic; i++)
                {
                    // Filter combined indexes, this binding only handles
                    // simple indexes.
                    //if (iis[i].attrIndexArr_1 != -1) continue;
                    //pb = GetPropertyBindingByDataIndex(iis[i].attrIndexArr_0);
                    // Check that the index is attached to the bound type and
                    // not an exstension or the extended type.
                    //if (pb._belongsTo != this) continue;
                    // If reference index we can't query on defined values, so
                    // we declare the index as containing only defined values.
                    //it = IndexType.Plain;
                    //if (pb.TypeCode == DbTypeCode.Object)
                    //    it = IndexType.Plain_OnlyDefined;
                    // ***
                    name = new String(iis[i].name);
                    // Get the number of attributes.
                    attributeCount = iis[i].attributeCount;
                    if (attributeCount < 1 || attributeCount > 10)
                    {
                        throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect attributeCount.");
                    }
                    // Get the sort orderings.
                    sortOrderings = new SortOrder[attributeCount];
                    tempSortMask = iis[i].sortMask;
                    for (Int32 j = 0; j < attributeCount; j++)
                    {
                        if ((tempSortMask & 1) == 1)
                        {
                            sortOrderings[j] = SortOrder.Descending;
                        }
                        else
                        {
                            sortOrderings[j] = SortOrder.Ascending;
                        }
                        tempSortMask = (UInt16)(tempSortMask >> 1);
                    }
                    // TODO: Check it is okay to use attributeCount instead of termination by value -1.
                    // Get the property bindings.
                    propertyBindings = new PropertyBinding[attributeCount];
                    nonBelongingPropertyBinding = false;
                    for (Int32 j = 0; j < attributeCount; j++)
                    {
                        switch (j)
                        {
                            case 0:
                                propertyBindings[j] = GetPropertyBindingByDataIndex(iis[i].attrIndexArr_0);
                                break;
                            case 1:
                                propertyBindings[j] = GetPropertyBindingByDataIndex(iis[i].attrIndexArr_1);
                                break;
                            case 2:
                                propertyBindings[j] = GetPropertyBindingByDataIndex(iis[i].attrIndexArr_2);
                                break;
                            case 3:
                                propertyBindings[j] = GetPropertyBindingByDataIndex(iis[i].attrIndexArr_3);
                                break;
                            case 4:
                                propertyBindings[j] = GetPropertyBindingByDataIndex(iis[i].attrIndexArr_4);
                                break;
                            case 5:
                                propertyBindings[j] = GetPropertyBindingByDataIndex(iis[i].attrIndexArr_5);
                                break;
                            case 6:
                                propertyBindings[j] = GetPropertyBindingByDataIndex(iis[i].attrIndexArr_6);
                                break;
                            case 7:
                                propertyBindings[j] = GetPropertyBindingByDataIndex(iis[i].attrIndexArr_7);
                                break;
                            case 8:
                                propertyBindings[j] = GetPropertyBindingByDataIndex(iis[i].attrIndexArr_8);
                                break;
                            case 9:
                                propertyBindings[j] = GetPropertyBindingByDataIndex(iis[i].attrIndexArr_9);
                                break;
                            case 10:
                                propertyBindings[j] = GetPropertyBindingByDataIndex(iis[i].attrIndexArr_10);
                                break;
                        }
#if false // TODO:
                        if (propertyBindings[j]._belongsTo != this)
                        {
                            nonBelongingPropertyBinding = true;
                            break;
                        }
#endif
                    }
                    if (!nonBelongingPropertyBinding)
                    {
                        iil.Add(new IndexInfo(iis[i].handle, name, this, propertyBindings, sortOrderings));
                    }
                }

                return iil.ToArray();
            }
        }

        public string Name
        {
            get { return TableDef.Name; }
        }

        public int PropertyCount
        {
            get { return TableDef.Columns.Length; }
        }

        public int GetPropertyIndex(string name)
        {
            throw new System.NotImplementedException();
        }

        public IPropertyBinding GetPropertyBinding(int index)
        {
            return GetPropertyBindingByDataIndex(index);
        }

        public PropertyBinding GetPropertyBindingByDataIndex(int index)
        {
            ColumnDef column = TableDef.Columns[index];
            return new PropertyBinding(column, index);
        }

        public IPropertyBinding GetPropertyBinding(string name)
        {
            for (int i = 0; i < TableDef.Columns.Length; i++)
            {
                if (TableDef.Columns[i].Name == name)
                {
                    return new PropertyBinding(TableDef.Columns[i], i);
                }
            }
            return null;
        }
    }
}
