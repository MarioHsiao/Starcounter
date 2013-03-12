// ***********************************************************************
// <copyright file="BindingBuilder.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Starcounter.Binding
{

    /// <summary>
    /// Class BindingBuilder
    /// </summary>
    internal sealed class BindingBuilder
    {

        /// <summary>
        /// The _type def
        /// </summary>
        private readonly TypeDef _typeDef;

        private readonly ushort[] _currentAndBaseTableIds;

        /// <summary>
        /// The _assembly name
        /// </summary>
        private readonly string _assemblyName;
        /// <summary>
        /// The _assembly builder
        /// </summary>
        private readonly AssemblyBuilder _assemblyBuilder;
        /// <summary>
        /// The _module builder
        /// </summary>
        private readonly ModuleBuilder _moduleBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="BindingBuilder" /> class.
        /// </summary>
        /// <param name="typeDef">The type def.</param>
        /// <param name="currentAndBaseTableIds">The baseTableIds.</param>
        internal BindingBuilder(TypeDef typeDef, ushort[] currentAndBaseTableIds)
        {
            _typeDef = typeDef;
            _currentAndBaseTableIds = currentAndBaseTableIds;

            _assemblyName = string.Concat("gen.", typeDef.Name);

            string builderOutputDir = AppDomain.CurrentDomain.BaseDirectory;
            
            Version version = new Version(1, 0, 1, 0);
            AssemblyName fullAssemblyName = new AssemblyName();
            fullAssemblyName.Name = _assemblyName;
            fullAssemblyName.Version = new Version(1, 0, 1, 0);
            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(fullAssemblyName, AssemblyBuilderAccess.RunAndSave, builderOutputDir);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(string.Concat(_assemblyName, ".dll"), string.Concat(_assemblyName, ".dll"));
        }

        /// <summary>
        /// Writes the assembly to disk.
        /// </summary>
        internal void WriteAssemblyToDisk()
        {
            _assemblyBuilder.Save(String.Concat(_assemblyBuilder.GetName().Name, ".dll"));
        }

        /// <summary>
        /// Creates the type binding.
        /// </summary>
        /// <returns>TypeBinding.</returns>
        internal TypeBinding CreateTypeBinding()
        {
            TypeDef typeDef;
            Type type;
            String typeBindingTypeName;
            TypeBuilder typeBuilder;
            MethodInfo methodInfo;
            MethodBuilder methodBuilder;
            ILGenerator ilGenerator;
            Type[] utypes;
            LocalBuilder localBuilder;
            Type typeBindingType;
            TypeBinding typeBinding;
            Type bindingBase;
            ConstructorInfo ctor;

            typeDef = _typeDef;
            type = typeDef.TypeLoader.Load();

            typeBindingTypeName = String.Concat(
                                      _assemblyName,
                                      ".",
                                      type.FullName,
                                      "_Binding"
                                  );
            bindingBase = typeof(TypeBinding);
            typeBuilder = _moduleBuilder.DefineType(
                              typeBindingTypeName,
                              (TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed),
                              bindingBase
                          );

            methodInfo = typeof(TypeBinding).GetMethod("NewUninitializedInst", (BindingFlags.Instance | BindingFlags.NonPublic));
            methodBuilder = typeBuilder.DefineMethod(
                                "NewUninitializedInst",
                                (MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.Family | MethodAttributes.Virtual),
                                typeof(IObjectView),
                                null
                            );
            typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
            ilGenerator = methodBuilder.GetILGenerator();
            ilGenerator.BeginScope();
            if (type.IsAbstract != true)
            {
                utypes = new Type[1];
                utypes[0] = typeof(Uninitialized);
                localBuilder = ilGenerator.DeclareLocal(utypes[0]);
                ilGenerator.Emit(OpCodes.Ldloca_S, localBuilder);
                ilGenerator.Emit(OpCodes.Initobj, utypes[0]);
                ilGenerator.Emit(OpCodes.Ldloc_0);
                ctor = type.GetConstructor(
                           (BindingFlags)BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                           null,
                           utypes,
                           null
                       );
                ilGenerator.Emit(OpCodes.Newobj, ctor);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldnull);
            }
            ilGenerator.Emit(OpCodes.Ret);
            ilGenerator.EndScope();

            // Generate type and create and initialize an instance of the type.
            
            typeBindingType = typeBuilder.CreateType();
            typeBinding = (TypeBinding)(typeBindingType.GetConstructor(Type.EmptyTypes).Invoke(null));
            typeBinding.Name = typeDef.Name;
            typeBinding.TableId = typeDef.TableDef.TableId;
            typeBinding.TypeDef = typeDef;
            typeBinding.SetCurrentAndBaseTableIds(_currentAndBaseTableIds);

            SetTypeBindingFlags(typeBinding, type);

            BuildPropertyBindingList(typeBinding, type);

            return typeBinding;
        }

        /// <summary>
        /// The entity type
        /// </summary>
        private static Type entityType = typeof(Entity);

        /// <summary>
        /// Sets the type binding flags.
        /// </summary>
        /// <param name="binding">The binding.</param>
        /// <param name="type">The type.</param>
        private void SetTypeBindingFlags(TypeBinding binding, Type type)
        {
            TypeBindingFlags bindingFlags = 0;

            while (type != entityType)
            {
                MethodInfo callbackMethod = type.GetMethod(
                                     "OnDelete",
                                     (BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)
                                 );
                if (callbackMethod == null)
                {
                    type = type.BaseType;
                }
                else
                {
                    bindingFlags |= TypeBindingFlags.Callback_OnDelete;
                    break;
                }
            }

            binding.Flags = bindingFlags;
        }


        /// <summary>
        /// Builds the property binding list.
        /// </summary>
        /// <param name="typeBinding">The type binding.</param>
        /// <param name="type">The type.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        private void BuildPropertyBindingList(TypeBinding typeBinding, Type type)
        {
            PropertyDef[] propertyDefs = typeBinding.TypeDef.PropertyDefs;

            PropertyBinding[] propertyBindings = new PropertyBinding[propertyDefs.Length];
            
            for (int i = 0; i < propertyDefs.Length; i++)
            {
                PropertyDef propertyDef = propertyDefs[i];
                PropertyBinding propertyBinding = null;

                switch (propertyDef.Type)
                {
                case DbTypeCode.Boolean:
                    propertyBinding = CreateBooleanPropertyBinding(propertyDef, type);
                    break;
                case DbTypeCode.Byte:
                    propertyBinding = CreateBytePropertyBinding(propertyDef, type);
                    break;
                case DbTypeCode.DateTime:
                    propertyBinding = CreateDateTimePropertyBinding(propertyDef, type);
                    break;
                case DbTypeCode.Decimal:
                    propertyBinding = CreateDecimalPropertyBinding(propertyDef, type);
                    break;
                case DbTypeCode.Single:
                    propertyBinding = CreateSinglePropertyBinding(propertyDef, type);
                    break;
                case DbTypeCode.Double:
                    propertyBinding = CreateDoublePropertyBinding(propertyDef, type);
                    break;
                case DbTypeCode.Int64:
                    propertyBinding = CreateInt64PropertyBinding(propertyDef, type);
                    break;
                case DbTypeCode.Int32:
                    propertyBinding = CreateInt32PropertyBinding(propertyDef, type);
                    break;
                case DbTypeCode.Int16:
                    propertyBinding = CreateInt16PropertyBinding(propertyDef, type);
                    break;
                case DbTypeCode.Object:
                    propertyBinding = CreateObjectPropertyBinding(propertyDef, type);
                    break;
                case DbTypeCode.SByte:
                    propertyBinding = CreateSBytePropertyBinding(propertyDef, type);
                    break;
                case DbTypeCode.String:
                    propertyBinding = CreateStringPropertyBinding(propertyDef, type);
                    break;
                case DbTypeCode.UInt64:
                    propertyBinding = CreateUInt64PropertyBinding(propertyDef, type);
                    break;
                case DbTypeCode.UInt32:
                    propertyBinding = CreateUInt32PropertyBinding(propertyDef, type);
                    break;
                case DbTypeCode.UInt16:
                    propertyBinding = CreateUInt16PropertyBinding(propertyDef, type);
                    break;
                case DbTypeCode.Binary:
                    propertyBinding = CreateBinaryPropertyBinding(propertyDef, type);
                    break;
                case DbTypeCode.LargeBinary:
                    propertyBinding = CreateLargeBinaryPropertyBinding(propertyDef, type);
                    break;
                default:
                    throw new NotSupportedException();
                }

                propertyBinding.SetDataIndex(propertyDef.ColumnIndex);
                propertyBinding.SetIndex(i);
                propertyBinding.SetName(propertyDef.Name);

                propertyBindings[i] = propertyBinding;
            }

            typeBinding.SetPropertyBindings(propertyBindings);
        }

        /// <summary>
        /// The bool property binding base type
        /// </summary>
        private static Type boolPropertyBindingBaseType = typeof(BooleanPropertyBinding);
        /// <summary>
        /// The bool property binding return type
        /// </summary>
        private static Type boolPropertyBindingReturnType = typeof(Boolean);
        /// <summary>
        /// The nullable bool property binding return type
        /// </summary>
        private static Type nullableBoolPropertyBindingReturnType = typeof(Nullable<Boolean>);

        /// <summary>
        /// Creates the boolean property binding.
        /// </summary>
        /// <param name="propertyDef">The property def.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <returns>PropertyBinding.</returns>
        private PropertyBinding CreateBooleanPropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            return GeneratePropertyBindingDefault(
                propertyDef,
                boolPropertyBindingBaseType,
                "DoGetBoolean",
                boolPropertyBindingReturnType,
                nullableBoolPropertyBindingReturnType,
                thisType
                );
        }

        /// <summary>
        /// The datetime property binding base type
        /// </summary>
        private static Type datetimePropertyBindingBaseType = typeof(DateTimePropertyBinding);
        /// <summary>
        /// The datetime property binding return type
        /// </summary>
        private static Type datetimePropertyBindingReturnType = typeof(DateTime);
        /// <summary>
        /// The nullable datetime property binding return type
        /// </summary>
        private static Type nullableDatetimePropertyBindingReturnType = typeof(Nullable<DateTime>);

        /// <summary>
        /// Creates the date time property binding.
        /// </summary>
        /// <param name="propertyDef">The property def.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <returns>PropertyBinding.</returns>
        private PropertyBinding CreateDateTimePropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            return GeneratePropertyBindingDefault(
                propertyDef,
                datetimePropertyBindingBaseType,
                "DoGetDateTime",
                datetimePropertyBindingReturnType,
                nullableDatetimePropertyBindingReturnType,
                thisType
                );
        }

        /// <summary>
        /// The decimal property binding base type
        /// </summary>
        private static Type decimalPropertyBindingBaseType = typeof(DecimalPropertyBinding);
        /// <summary>
        /// The decimal property binding return type
        /// </summary>
        private static Type decimalPropertyBindingReturnType = typeof(Decimal);
        /// <summary>
        /// The nullable decimal property binding return type
        /// </summary>
        private static Type nullableDecimalPropertyBindingReturnType = typeof(Nullable<Decimal>);

        /// <summary>
        /// Creates the decimal property binding.
        /// </summary>
        /// <param name="propertyDef">The property def.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <returns>PropertyBinding.</returns>
        private PropertyBinding CreateDecimalPropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            return GeneratePropertyBindingDefault(
                propertyDef,
                decimalPropertyBindingBaseType,
                "DoGetDecimal",
                decimalPropertyBindingReturnType,
                nullableDecimalPropertyBindingReturnType,
                thisType
                );
        }

        /// <summary>
        /// The double property binding base type
        /// </summary>
        private static Type doublePropertyBindingBaseType = typeof(DoublePropertyBinding);
        /// <summary>
        /// The double property binding return type
        /// </summary>
        private static Type doublePropertyBindingReturnType = typeof(Double);
        /// <summary>
        /// The nullable double property binding return type
        /// </summary>
        private static Type nullableDoublePropertyBindingReturnType = typeof(Nullable<Double>);

        /// <summary>
        /// Creates the double property binding.
        /// </summary>
        /// <param name="propertyDef">The property def.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <returns>PropertyBinding.</returns>
        private PropertyBinding CreateDoublePropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            return GeneratePropertyBindingDefault(
                propertyDef,
                doublePropertyBindingBaseType,
                "DoGetDouble",
                doublePropertyBindingReturnType,
                nullableDoublePropertyBindingReturnType,
                thisType
                );
        }

        /// <summary>
        /// The single property binding base type
        /// </summary>
        private static Type singlePropertyBindingBaseType = typeof(SinglePropertyBinding);
        /// <summary>
        /// The single property binding return type
        /// </summary>
        private static Type singlePropertyBindingReturnType = typeof(Single);
        /// <summary>
        /// The nullable single property binding return type
        /// </summary>
        private static Type nullableSinglePropertyBindingReturnType = typeof(Nullable<Single>);

        /// <summary>
        /// Creates the single property binding.
        /// </summary>
        /// <param name="propertyDef">The property def.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <returns>PropertyBinding.</returns>
        private PropertyBinding CreateSinglePropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            return GeneratePropertyBindingDefault(
                propertyDef,
                singlePropertyBindingBaseType,
                "DoGetSingle",
                singlePropertyBindingReturnType,
                nullableSinglePropertyBindingReturnType,
                thisType
                );
        }

        /// <summary>
        /// The object property binding base type
        /// </summary>
        private static Type objectPropertyBindingBaseType = typeof(ObjectPropertyBinding);
        /// <summary>
        /// The object property binding return type
        /// </summary>
        private static Type objectPropertyBindingReturnType = typeof(Entity);

        /// <summary>
        /// Creates the object property binding.
        /// </summary>
        /// <param name="propertyDef">The property def.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <returns>PropertyBinding.</returns>
        private PropertyBinding CreateObjectPropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            PropertyInfo propertyInfo;
            String propBindingTypeName;
            TypeBuilder typeBuilder;
            Type propBindingType;
            ObjectPropertyBinding propertyBinding;

            propertyInfo = thisType.GetProperty(propertyDef.Name, BindingFlags.Public | BindingFlags.Instance);
            VerifyObjectProperty(propertyInfo);

            propBindingTypeName = String.Concat(
                                      _assemblyName,
                                      ".",
                                      thisType.FullName,
                                      "_",
                                      propertyInfo.Name,
                                      "_Binding"
                                  );
            typeBuilder = _moduleBuilder.DefineType(
                              propBindingTypeName,
                              (TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed),
                              objectPropertyBindingBaseType
                          );
            GeneratePropertyBindingNoNullOut(typeBuilder, "DoGetObject", objectPropertyBindingReturnType, thisType, propertyInfo);
            propBindingType = typeBuilder.CreateType();
            propertyBinding = (ObjectPropertyBinding)(propBindingType.GetConstructor(Type.EmptyTypes).Invoke(null));

            propertyBinding.SetTargetTypeName(propertyDef.TargetTypeName);

            return propertyBinding;
        }

        /// <summary>
        /// The int8 property binding base type
        /// </summary>
        private static Type int8PropertyBindingBaseType = typeof(SBytePropertyBinding);
        /// <summary>
        /// The int8 property binding return type
        /// </summary>
        private static Type int8PropertyBindingReturnType = typeof(SByte);
        /// <summary>
        /// The nullable int8 property binding return type
        /// </summary>
        private static Type nullableInt8PropertyBindingReturnType = typeof(Nullable<SByte>);

        /// <summary>
        /// Creates the S byte property binding.
        /// </summary>
        /// <param name="propertyDef">The property def.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <returns>PropertyBinding.</returns>
        private PropertyBinding CreateSBytePropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            return GeneratePropertyBindingDefault(
                propertyDef,
                int8PropertyBindingBaseType,
                "DoGetSByte",
                int8PropertyBindingReturnType,
                nullableInt8PropertyBindingReturnType,
                thisType
                );
        }

        /// <summary>
        /// The int16 property binding base type
        /// </summary>
        private static Type int16PropertyBindingBaseType = typeof(Int16PropertyBinding);
        /// <summary>
        /// The int16 property binding return type
        /// </summary>
        private static Type int16PropertyBindingReturnType = typeof(Int16);
        /// <summary>
        /// The nullable int16 property binding return type
        /// </summary>
        private static Type nullableInt16PropertyBindingReturnType = typeof(Nullable<Int16>);

        /// <summary>
        /// Creates the int16 property binding.
        /// </summary>
        /// <param name="propertyDef">The property def.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <returns>PropertyBinding.</returns>
        private PropertyBinding CreateInt16PropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            return GeneratePropertyBindingDefault(
                propertyDef,
                int16PropertyBindingBaseType,
                "DoGetInt16",
                int16PropertyBindingReturnType,
                nullableInt16PropertyBindingReturnType,
                thisType
                );
        }

        /// <summary>
        /// The int32 property binding base type
        /// </summary>
        private static Type int32PropertyBindingBaseType = typeof(Int32PropertyBinding);
        /// <summary>
        /// The int32 property binding return type
        /// </summary>
        private static Type int32PropertyBindingReturnType = typeof(Int32);
        /// <summary>
        /// The nullable int32 property binding return type
        /// </summary>
        private static Type nullableInt32PropertyBindingReturnType = typeof(Nullable<Int32>);

        /// <summary>
        /// Creates the int32 property binding.
        /// </summary>
        /// <param name="propertyDef">The property def.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <returns>PropertyBinding.</returns>
        private PropertyBinding CreateInt32PropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            return GeneratePropertyBindingDefault(
                propertyDef,
                int32PropertyBindingBaseType,
                "DoGetInt32",
                int32PropertyBindingReturnType,
                nullableInt32PropertyBindingReturnType,
                thisType
                );
        }

        /// <summary>
        /// The int64 property binding base type
        /// </summary>
        private static Type int64PropertyBindingBaseType = typeof(Int64PropertyBinding);
        /// <summary>
        /// The int64 property binding return type
        /// </summary>
        private static Type int64PropertyBindingReturnType = typeof(Int64);
        /// <summary>
        /// The nullable int64 property binding return type
        /// </summary>
        private static Type nullableInt64PropertyBindingReturnType = typeof(Nullable<Int64>);

        /// <summary>
        /// Creates the int64 property binding.
        /// </summary>
        /// <param name="propertyDef">The property def.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <returns>PropertyBinding.</returns>
        private PropertyBinding CreateInt64PropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            return GeneratePropertyBindingDefault(
                propertyDef,
                int64PropertyBindingBaseType,
                "DoGetInt64",
                int64PropertyBindingReturnType,
                nullableInt64PropertyBindingReturnType,
                thisType
                );
        }

        /// <summary>
        /// The string property binding base type
        /// </summary>
        private static Type stringPropertyBindingBaseType = typeof(StringPropertyBinding);
        /// <summary>
        /// The string property binding return type
        /// </summary>
        private static Type stringPropertyBindingReturnType = typeof(String);

        /// <summary>
        /// Creates the string property binding.
        /// </summary>
        /// <param name="propertyDef">The property def.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <returns>PropertyBinding.</returns>
        private PropertyBinding CreateStringPropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            return GeneratePropertyBindingNoNullOut(
                propertyDef,
                stringPropertyBindingBaseType,
                "DoGetString",
                stringPropertyBindingReturnType,
                thisType
                );
        }

        /// <summary>
        /// The uint8 property binding base type
        /// </summary>
        private static Type uint8PropertyBindingBaseType = typeof(BytePropertyBinding);
        /// <summary>
        /// The uint8 property binding return type
        /// </summary>
        private static Type uint8PropertyBindingReturnType = typeof(Byte);
        /// <summary>
        /// The nullable uint8 property binding return type
        /// </summary>
        private static Type nullableUint8PropertyBindingReturnType = typeof(Nullable<Byte>);

        /// <summary>
        /// Creates the byte property binding.
        /// </summary>
        /// <param name="propertyDef">The property def.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <returns>PropertyBinding.</returns>
        private PropertyBinding CreateBytePropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            return GeneratePropertyBindingDefault(
                propertyDef,
                uint8PropertyBindingBaseType,
                "DoGetByte",
                uint8PropertyBindingReturnType,
                nullableUint8PropertyBindingReturnType,
                thisType
                );
        }

        /// <summary>
        /// The uint16 property binding base type
        /// </summary>
        private static Type uint16PropertyBindingBaseType = typeof(UInt16PropertyBinding);
        /// <summary>
        /// The uint16 property binding return type
        /// </summary>
        private static Type uint16PropertyBindingReturnType = typeof(UInt16);
        /// <summary>
        /// The nullable uint16 property binding return type
        /// </summary>
        private static Type nullableUint16PropertyBindingReturnType = typeof(Nullable<UInt16>);

        /// <summary>
        /// Creates the U int16 property binding.
        /// </summary>
        /// <param name="propertyDef">The property def.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <returns>PropertyBinding.</returns>
        private PropertyBinding CreateUInt16PropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            return GeneratePropertyBindingDefault(
                propertyDef,
                uint16PropertyBindingBaseType,
                "DoGetUInt16",
                uint16PropertyBindingReturnType,
                nullableUint16PropertyBindingReturnType,
                thisType
                );
        }

        /// <summary>
        /// The uint32 property binding base type
        /// </summary>
        private static Type uint32PropertyBindingBaseType = typeof(UInt32PropertyBinding);
        /// <summary>
        /// The uint32 property binding return type
        /// </summary>
        private static Type uint32PropertyBindingReturnType = typeof(UInt32);
        /// <summary>
        /// The nullable uint32 property binding return type
        /// </summary>
        private static Type nullableUint32PropertyBindingReturnType = typeof(Nullable<UInt32>);

        /// <summary>
        /// Creates the U int32 property binding.
        /// </summary>
        /// <param name="propertyDef">The property def.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <returns>PropertyBinding.</returns>
        private PropertyBinding CreateUInt32PropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            return GeneratePropertyBindingDefault(
                propertyDef,
                uint32PropertyBindingBaseType,
                "DoGetUInt32",
                uint32PropertyBindingReturnType,
                nullableUint32PropertyBindingReturnType,
                thisType
                );
        }

        /// <summary>
        /// The uint64 property binding base type
        /// </summary>
        private static Type uint64PropertyBindingBaseType = typeof(UInt64PropertyBinding);
        /// <summary>
        /// The uint64 property binding return type
        /// </summary>
        private static Type uint64PropertyBindingReturnType = typeof(UInt64);
        /// <summary>
        /// The nullable uint64 property binding return type
        /// </summary>
        private static Type nullableUint64PropertyBindingReturnType = typeof(Nullable<UInt64>);

        /// <summary>
        /// Creates the U int64 property binding.
        /// </summary>
        /// <param name="propertyDef">The property def.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <returns>PropertyBinding.</returns>
        private PropertyBinding CreateUInt64PropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            return GeneratePropertyBindingDefault(
                propertyDef,
                uint64PropertyBindingBaseType,
                "DoGetUInt64",
                uint64PropertyBindingReturnType,
                nullableUint64PropertyBindingReturnType,
                thisType
                );
        }

        /// <summary>
        /// The binary property binding base type
        /// </summary>
        private static Type binaryPropertyBindingBaseType = typeof(BinaryPropertyBinding);
        /// <summary>
        /// The binary property binding return type
        /// </summary>
        private static Type binaryPropertyBindingReturnType = typeof(Binary);

        /// <summary>
        /// Creates the binary property binding.
        /// </summary>
        /// <param name="propertyDef">The property def.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <returns>PropertyBinding.</returns>
        private PropertyBinding CreateBinaryPropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            return GeneratePropertyBindingNoNullOut(
                propertyDef,
                binaryPropertyBindingBaseType,
                "DoGetBinary",
                binaryPropertyBindingReturnType,
                thisType
                );
        }

        /// <summary>
        /// The largebinary property binding return type
        /// </summary>
        private static Type largebinaryPropertyBindingReturnType = typeof(LargeBinary);

        /// <summary>
        /// Creates the large binary property binding.
        /// </summary>
        /// <param name="propertyDef">The property def.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <returns>PropertyBinding.</returns>
        private PropertyBinding CreateLargeBinaryPropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            PropertyInfo propertyInfo = thisType.GetProperty(propertyDef.Name, BindingFlags.Public | BindingFlags.Instance);
            VerifyProperty(propertyInfo, largebinaryPropertyBindingReturnType);
            return new LargeBinaryPropertyBinding();
        }

        /// <summary>
        /// Generates the property binding default.
        /// </summary>
        /// <param name="propertyDef">The property def.</param>
        /// <param name="bindingBaseType">Type of the binding base.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="returnType">Type of the return.</param>
        /// <param name="nullableReturnType">Type of the nullable return.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <returns>PropertyBinding.</returns>
        private PropertyBinding GeneratePropertyBindingDefault(PropertyDef propertyDef, Type bindingBaseType, String methodName, Type returnType, Type nullableReturnType, Type thisType)
        {
            PropertyInfo propertyInfo;
            bool isNullable;
            String propBindingTypeName;
            TypeBuilder typeBuilder;
            Type propBindingType;

            propertyInfo = thisType.GetProperty(propertyDef.Name, BindingFlags.Public | BindingFlags.Instance);

            isNullable = propertyDef.IsNullable;

            Type implReturnType = nullableReturnType;
            Type targetReturnType = !isNullable ? returnType : implReturnType;
            VerifyProperty(propertyInfo, targetReturnType);

            propBindingTypeName = String.Concat(
                                      _assemblyName,
                                      ".",
                                      thisType.FullName,
                                      "_",
                                      propertyInfo.Name,
                                      "_Binding"
                                  );
            typeBuilder = _moduleBuilder.DefineType(
                              propBindingTypeName,
                              (TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed),
                              bindingBaseType
                          );
            if (!isNullable)
            {
                GeneratePropertyBindingDefault(typeBuilder, methodName, implReturnType, targetReturnType, thisType, propertyInfo);
            }
            else
            {
                GeneratePropertyBindingDefaultNullable(typeBuilder, methodName, targetReturnType, thisType, propertyInfo);
            }
            propBindingType = typeBuilder.CreateType();
            return (PropertyBinding)(propBindingType.GetConstructor(Type.EmptyTypes).Invoke(null));
        }

        /// <summary>
        /// The property binding get params
        /// </summary>
        private static Type[] propertyBindingGetParams = new Type[] { typeof(Object) };

        /// <summary>
        /// Generates the property binding default.
        /// </summary>
        /// <param name="typeBuilder">The type builder.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="implReturnType">Type of the impl return.</param>
        /// <param name="targetReturnType">Type of the target return.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <param name="propertyInfo">The property info.</param>
        private void GeneratePropertyBindingDefault(TypeBuilder typeBuilder, String methodName, Type implReturnType, Type targetReturnType, Type thisType, PropertyInfo propertyInfo)
        {
            MethodInfo methodInfo;
            MethodBuilder methodBuilder;
            ILGenerator ilGenerator;

            ConstructorInfo implReturnTypeCtor = implReturnType.GetConstructor(new Type[] { targetReturnType });

            methodInfo = typeof(PropertyBinding).GetMethod(
                             methodName,
                             (BindingFlags.Instance | BindingFlags.NonPublic)
                         );
            methodBuilder = typeBuilder.DefineMethod(
                                methodName,
                                (MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.Family | MethodAttributes.Virtual),
                                implReturnType,
                                propertyBindingGetParams
                            );
            typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
            ilGenerator = methodBuilder.GetILGenerator();
            ilGenerator.BeginScope();
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Castclass, thisType);
            ilGenerator.Emit(OpCodes.Callvirt, propertyInfo.GetGetMethod());
            ilGenerator.Emit(OpCodes.Newobj, implReturnTypeCtor);
            ilGenerator.Emit(OpCodes.Ret);
            ilGenerator.EndScope();
        }

        /// <summary>
        /// Generates the property binding default nullable.
        /// </summary>
        /// <param name="typeBuilder">The type builder.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="returnType">Type of the return.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <param name="propertyInfo">The property info.</param>
        private void GeneratePropertyBindingDefaultNullable(TypeBuilder typeBuilder, String methodName, Type returnType, Type thisType, PropertyInfo propertyInfo)
        {
            MethodInfo methodInfo;
            MethodBuilder methodBuilder;
            ILGenerator ilGenerator;
            
            methodInfo = typeof(PropertyBinding).GetMethod(
                             methodName,
                             (BindingFlags.Instance | BindingFlags.NonPublic)
                         );
            methodBuilder = typeBuilder.DefineMethod(
                                methodName,
                                (MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.Family | MethodAttributes.Virtual),
                                returnType,
                                propertyBindingGetParams
                            );
            typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
            ilGenerator = methodBuilder.GetILGenerator();
            ilGenerator.BeginScope();
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Castclass, thisType);
            ilGenerator.Emit(OpCodes.Callvirt, propertyInfo.GetGetMethod());
            ilGenerator.Emit(OpCodes.Ret);
            ilGenerator.EndScope();
        }

        /// <summary>
        /// Generates the property binding no null out.
        /// </summary>
        /// <param name="propertyDef">The property def.</param>
        /// <param name="bindingBaseType">Type of the binding base.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="returnType">Type of the return.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <returns>PropertyBinding.</returns>
        private PropertyBinding GeneratePropertyBindingNoNullOut(PropertyDef propertyDef, Type bindingBaseType, String methodName, Type returnType, Type thisType)
        {
            PropertyInfo propertyInfo;
            String propBindingTypeName;
            TypeBuilder typeBuilder;
            Type propBindingType;

            propertyInfo = thisType.GetProperty(propertyDef.Name, BindingFlags.Public | BindingFlags.Instance);
            VerifyProperty(propertyInfo, returnType);

            propBindingTypeName = String.Concat(
                                      _assemblyName,
                                      ".",
                                      thisType.FullName,
                                      "_",
                                      propertyInfo.Name,
                                      "_Binding"
                                  );
            typeBuilder = _moduleBuilder.DefineType(
                              propBindingTypeName,
                              (TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed),
                              bindingBaseType
                          );
            GeneratePropertyBindingNoNullOut(typeBuilder, methodName, returnType, thisType, propertyInfo);
            propBindingType = typeBuilder.CreateType();
            return (PropertyBinding)(propBindingType.GetConstructor(Type.EmptyTypes).Invoke(null));
        }

        /// <summary>
        /// Generates the property binding no null out.
        /// </summary>
        /// <param name="typeBuilder">The type builder.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="returnType">Type of the return.</param>
        /// <param name="thisType">Type of the this.</param>
        /// <param name="propertyInfo">The property info.</param>
        private void GeneratePropertyBindingNoNullOut(TypeBuilder typeBuilder, String methodName, Type returnType, Type thisType, PropertyInfo propertyInfo)
        {
            MethodInfo methodInfo;
            MethodBuilder methodBuilder;
            ILGenerator ilGenerator;
            Type[] paramTypeArray;
            paramTypeArray = new Type[1];
            paramTypeArray[0] = typeof(Object);
            methodInfo = typeof(PropertyBinding).GetMethod(
                             methodName,
                             (BindingFlags.Instance | BindingFlags.NonPublic)
                         );
            methodBuilder = typeBuilder.DefineMethod(
                                methodName,
                                (MethodAttributes.HideBySig | MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.Family | MethodAttributes.Virtual),
                                returnType,
                                paramTypeArray
                            );
            typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
            ilGenerator = methodBuilder.GetILGenerator();
            ilGenerator.BeginScope();
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Castclass, thisType);
            ilGenerator.Emit(OpCodes.Callvirt, propertyInfo.GetGetMethod());
            ilGenerator.Emit(OpCodes.Ret);
            ilGenerator.EndScope();
        }

        /// <summary>
        /// Verifies the property.
        /// </summary>
        /// <param name="propertyInfo">The property info.</param>
        /// <param name="returnType">Type of the return.</param>
        private void VerifyProperty(PropertyInfo propertyInfo, Type returnType)
        {
            var propertyType = propertyInfo.PropertyType;
            if (
                propertyInfo != null &&
                propertyInfo.CanRead &&
                propertyType == returnType
                )
            {
                return;
            }
            
            Type underlyingType;
            if (returnType.IsGenericType)
            {
                if (
                    propertyType.IsGenericType &&
                    propertyType.GetGenericTypeDefinition().FullName == "System.Nullable`1"
                    )
                {
                    var propertyType2 = propertyType.GetGenericArguments()[0];
                    if (propertyType2.IsEnum)
                    {
                        underlyingType = propertyType2.GetEnumUnderlyingType();
                        var returnType2 = returnType.GetGenericArguments()[0];
                        if (underlyingType == returnType2) return;
                    }
                }
            }
            else
            {
                if (propertyType.IsEnum) 
                {
                    underlyingType = propertyType.GetEnumUnderlyingType();
                    if (underlyingType == returnType) return;
                }
            }

            throw ErrorCode.ToException(Error.SCERRSCHEMACODEMISMATCH, "VerifyProperty failed.");
        }

        /// <summary>
        /// Verifies the object property.
        /// </summary>
        /// <param name="propertyInfo">The property info.</param>
        private void VerifyObjectProperty(PropertyInfo propertyInfo)
        {
            if (
                propertyInfo != null &&
                propertyInfo.CanRead &&
                objectPropertyBindingReturnType.IsAssignableFrom(propertyInfo.PropertyType)
                )
                return;
            throw ErrorCode.ToException(Error.SCERRSCHEMACODEMISMATCH, "VerifyObjectProperty failed.");
        }
    }
}
