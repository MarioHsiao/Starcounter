
using Sc.Server.Binding;
using Sc.Server.Internal;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Starcounter.Binding
{
    
    internal sealed class BindingBuilder
    {

        private readonly TypeDef _typeDef;
        private readonly string _assemblyName;
        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly ModuleBuilder _moduleBuilder;

        internal BindingBuilder(TypeDef typeDef)
        {
            _typeDef = typeDef;

            _assemblyName = string.Concat("gen.", typeDef.Name);

            string builderOutputDir = AppDomain.CurrentDomain.BaseDirectory;
            
            Version version = new Version(1, 0, 1, 0);
            AssemblyName fullAssemblyName = new AssemblyName();
            fullAssemblyName.Name = _assemblyName;
            fullAssemblyName.Version = new Version(1, 0, 1, 0);
            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(fullAssemblyName, AssemblyBuilderAccess.RunAndSave, builderOutputDir);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(string.Concat(_assemblyName, ".dll"), string.Concat(_assemblyName, ".dll"));
        }

        internal void WriteAssemblyToDisk()
        {
            _assemblyBuilder.Save(String.Concat(_assemblyBuilder.GetName().Name, ".dll"));
        }

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
                                typeof(Entity),
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

            SetTypeBindingFlags(typeBinding, type);

            BuildPropertyBindingList(typeBinding, type);

            return typeBinding;
        }

        private static Type entityType = typeof(Entity);

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

        private static Type boolPropertyBindingBaseType = typeof(BooleanPropertyBinding);
        private static Type boolPropertyBindingReturnType = typeof(Boolean);
        private static Type nullableBoolPropertyBindingReturnType = typeof(Nullable<Boolean>);

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

        private static Type datetimePropertyBindingBaseType = typeof(DateTimePropertyBinding);
        private static Type datetimePropertyBindingReturnType = typeof(DateTime);
        private static Type nullableDatetimePropertyBindingReturnType = typeof(Nullable<DateTime>);

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

        private static Type decimalPropertyBindingBaseType = typeof(DecimalPropertyBinding);
        private static Type decimalPropertyBindingReturnType = typeof(Decimal);
        private static Type nullableDecimalPropertyBindingReturnType = typeof(Nullable<Decimal>);

        private PropertyBinding CreateDecimalPropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            return GeneratePropertyBindingDefault(
                propertyDef,
                decimalPropertyBindingBaseType,
                "DoDecimalTime",
                decimalPropertyBindingReturnType,
                nullableDecimalPropertyBindingReturnType,
                thisType
                );
        }

        private static Type doublePropertyBindingBaseType = typeof(DoublePropertyBinding);
        private static Type doublePropertyBindingReturnType = typeof(Double);
        private static Type nullableDoublePropertyBindingReturnType = typeof(Nullable<Double>);

        private PropertyBinding CreateDoublePropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            return GeneratePropertyBindingDefault(
                propertyDef,
                decimalPropertyBindingBaseType,
                "DoDoubleTime",
                decimalPropertyBindingReturnType,
                nullableDoublePropertyBindingReturnType,
                thisType
                );
        }

        private static Type singlePropertyBindingBaseType = typeof(SinglePropertyBinding);
        private static Type singlePropertyBindingReturnType = typeof(Single);
        private static Type nullableSinglePropertyBindingReturnType = typeof(Nullable<Single>);

        private PropertyBinding CreateSinglePropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            return GeneratePropertyBindingDefault(
                propertyDef,
                singlePropertyBindingBaseType,
                "DoSingleTime",
                singlePropertyBindingReturnType,
                nullableSinglePropertyBindingReturnType,
                thisType
                );
        }

        private static Type objectPropertyBindingBaseType = typeof(ObjectPropertyBinding);
        private static Type objectPropertyBindingReturnType = typeof(Entity);

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

        private static Type int8PropertyBindingBaseType = typeof(SBytePropertyBinding);
        private static Type int8PropertyBindingReturnType = typeof(SByte);
        private static Type nullableInt8PropertyBindingReturnType = typeof(Nullable<SByte>);

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

        private static Type int16PropertyBindingBaseType = typeof(Int16PropertyBinding);
        private static Type int16PropertyBindingReturnType = typeof(Int16);
        private static Type nullableInt16PropertyBindingReturnType = typeof(Nullable<Int16>);

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

        private static Type int32PropertyBindingBaseType = typeof(Int32PropertyBinding);
        private static Type int32PropertyBindingReturnType = typeof(Int32);
        private static Type nullableInt32PropertyBindingReturnType = typeof(Nullable<Int32>);

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

        private static Type int64PropertyBindingBaseType = typeof(Int64PropertyBinding);
        private static Type int64PropertyBindingReturnType = typeof(Int64);
        private static Type nullableInt64PropertyBindingReturnType = typeof(Nullable<Int64>);

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

        private static Type stringPropertyBindingBaseType = typeof(StringPropertyBinding);
        private static Type stringPropertyBindingReturnType = typeof(String);

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

        private static Type uint8PropertyBindingBaseType = typeof(BytePropertyBinding);
        private static Type uint8PropertyBindingReturnType = typeof(Byte);
        private static Type nullableUint8PropertyBindingReturnType = typeof(Nullable<Byte>);

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

        private static Type uint16PropertyBindingBaseType = typeof(UInt16PropertyBinding);
        private static Type uint16PropertyBindingReturnType = typeof(UInt16);
        private static Type nullableUint16PropertyBindingReturnType = typeof(Nullable<UInt16>);

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

        private static Type uint32PropertyBindingBaseType = typeof(UInt32PropertyBinding);
        private static Type uint32PropertyBindingReturnType = typeof(UInt32);
        private static Type nullableUint32PropertyBindingReturnType = typeof(Nullable<UInt32>);

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

        private static Type uint64PropertyBindingBaseType = typeof(UInt64PropertyBinding);
        private static Type uint64PropertyBindingReturnType = typeof(UInt64);
        private static Type nullableUint64PropertyBindingReturnType = typeof(Nullable<UInt64>);

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

        private static Type binaryPropertyBindingBaseType = typeof(BinaryPropertyBinding);
        private static Type binaryPropertyBindingReturnType = typeof(Binary);

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

        private static Type largebinaryPropertyBindingReturnType = typeof(LargeBinary);

        private PropertyBinding CreateLargeBinaryPropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            PropertyInfo propertyInfo = thisType.GetProperty(propertyDef.Name, BindingFlags.Public | BindingFlags.Instance);
            VerifyProperty(propertyInfo, largebinaryPropertyBindingReturnType);
            return new LargeBinaryPropertyBinding();
        }

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

        private static Type[] propertyBindingGetParams = new Type[] { typeof(Object) };

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

        private void VerifyProperty(PropertyInfo propertyInfo, Type returnType)
        {
            if (
                propertyInfo != null &&
                propertyInfo.CanRead &&
                propertyInfo.PropertyType == returnType
                )
                return;
            throw ErrorCode.ToException(Error.SCERRSCHEMACODEMISMATCH, "VerifyProperty failed.");
        }

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
