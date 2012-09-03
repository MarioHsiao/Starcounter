
using Sc.Server.Binding;
using Sc.Server.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Starcounter.Binding
{
    
    internal sealed class BindingBuilder
    {

        string _assemblyName;
        AssemblyBuilder _assemblyBuilder;
        ModuleBuilder _moduleBuilder;

        internal BindingBuilder()
        {
            _assemblyName = "kalle";

            string builderOutputDir = AppDomain.CurrentDomain.BaseDirectory;
            
            Version version = new Version(1, 0, 1, 0);
            AssemblyName fullAssemblyName = new AssemblyName();
            fullAssemblyName.Name = _assemblyName;
            fullAssemblyName.Version = new Version(1, 0, 1, 0);
            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(fullAssemblyName, AssemblyBuilderAccess.RunAndSave, builderOutputDir);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(string.Concat(_assemblyName, ".dll"), string.Concat(_assemblyName, ".dll"));
        }

        internal void BuildCompleted()
        {
#if true
            _assemblyBuilder.Save(String.Concat(_assemblyBuilder.GetName().Name, ".dll"));
#endif
        }

        internal TypeBinding CreateTypeBinding(TypeDef typeDef)
        {
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
            typeBinding.TypeDef = typeDef;

            BuildPropertyBindingList(typeBinding, type);

            return typeBinding;
        }

        private void BuildPropertyBindingList(TypeBinding typeBinding, Type type)
        {
            PropertyDef[] propertyDefs = typeBinding.TypeDef.PropertyDefs;

            Dictionary<string, PropertyBinding> propertyBindingsByName = new Dictionary<string, PropertyBinding>(propertyDefs.Length);
            
            for (int i = 0; i < propertyDefs.Length; i++)
            {
                PropertyDef propertyDef = propertyDefs[i];
                PropertyBinding propertyBinding = null;

                switch (propertyDef.Type)
                {
                case DbTypeCode.Int64:
                    propertyBinding = CreateInt64PropertyBinding(propertyDef, type);
                    break;
                case DbTypeCode.String:
                    propertyBinding = CreateStringPropertyBinding(propertyDef, type);
                    break;
                default:
                    throw new NotImplementedException();
                }

                propertyBinding.SetDataIndex(i); // TODO:
                propertyBinding.SetIndex(i);
                propertyBinding.SetName(propertyDef.Name);

                propertyBindingsByName.Add(propertyDef.Name, propertyBinding);
            }

            typeBinding.SetPropertyBindings(propertyBindingsByName);
        }

        private static Type int64PropertyBindingBaseType = typeof(Int64PropertyBinding);
        private static Type int64PropertyBindingReturnType = typeof(Int64);

        private PropertyBinding CreateInt64PropertyBinding(PropertyDef propertyDef, Type thisType)
        {
            return GeneratePropertyBindingDefault(
                propertyDef,
                int64PropertyBindingBaseType,
                "DoGetInt64",
                int64PropertyBindingReturnType,
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

        private PropertyBinding GeneratePropertyBindingDefault(PropertyDef propertyDef, Type bindingBaseType, String methodName, Type returnType, Type thisType)
        {
            PropertyInfo propertyInfo;
            String propBindingTypeName;
            TypeBuilder typeBuilder;
            Type propBindingType;

            // TODO: Handle nullable.

            propertyInfo = thisType.GetProperty(propertyDef.Name, BindingFlags.Public | BindingFlags.Instance);
            // TODO: Verify property (exists, readable, correct return type etc.).

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
            GeneratePropertyBindingDefault(typeBuilder, methodName, returnType, thisType, propertyInfo);
            propBindingType = typeBuilder.CreateType();
            return (PropertyBinding)(propBindingType.GetConstructor(Type.EmptyTypes).Invoke(null));
        }

        private void GeneratePropertyBindingDefault(TypeBuilder typeBuilder, String methodName, Type returnType, Type thisType, PropertyInfo propertyInfo)
        {
            MethodInfo methodInfo;
            MethodBuilder methodBuilder;
            ILGenerator ilGenerator;
            Type[] paramTypeArray;
            paramTypeArray = new Type[2];
            paramTypeArray[0] = typeof(Object);
            paramTypeArray[1] = typeof(Boolean).MakeByRefType();
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
            ilGenerator.Emit(OpCodes.Ldarg_2);
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Stind_I1);
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
            // TODO: Verify property (exists, readable, correct return type etc.).

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
    }
}
