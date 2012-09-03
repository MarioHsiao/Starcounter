
using Sc.Server.Binding;
using Sc.Server.Internal;
using System;
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
                                      typeDef.Name,
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
            
//            BuildPropertyBindingList(dpc, typeBinding, baseBinding);

            return typeBinding;
        }
    }
}
