using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using PostSharp.Extensibility;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.Binding;
using PostSharp.Sdk.CodeModel.Collections;
using PostSharp.Sdk.CodeModel.Helpers;
using PostSharp.Sdk.CodeModel.SerializationTypes;
using PostSharp.Sdk.CodeModel.TypeSignatures;
using PostSharp.Sdk.CodeWeaver;
using PostSharp.Sdk.Collections;
using PostSharp.Sdk.Extensibility;
using PostSharp.Sdk.Extensibility.Tasks;
using Sc.Server.Internal;
using Sc.Server.Weaver.Schema;
using Starcounter;
using Starcounter.Configuration;
using Starcounter.LucentObjects;
using IMember = PostSharp.Sdk.CodeModel.IMember;
using IMethod = PostSharp.Sdk.CodeModel.IMethod;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// PostSharp task responsible for transforming the assembly. It also implements the
    ///  <see cref="IAdviceProvider"/> interface to provide advices to the low-level
    /// code weaver.
    /// </summary>
    public class ScTransformTask : Task, IAdviceProvider {
        private static readonly TagId _constructorEnhancedTagGuid
                                        = TagId.Register("{A7296EEE-BD8D-4220-9153-B5AAE974FA98}");

        private readonly Dictionary<String, MethodPair> _fieldAccessors;
        private readonly InstructionWriter _writer;
        private readonly List<MethodDefDeclaration> _staticConstructors;
        private readonly List<InsteadOfFieldAccessAdvice> _fieldAdvices;
        private readonly List<IMethodLevelAdvice> _methodAdvices;
        private readonly List<ReimplementWeavedLucentAccessorAdvice> _weavedLucentAccessorAdvices;

        private CastHelper _castHelper;
        private DbStateMethodProvider _dbStateMethodProvider;
        private IMethod _adapterGetPropertyMethod;
        private IMethod _adapterResolveIndexMethod;
        private IMethod _objectConstructor;
        private IMethod _objViewPropIndexAttrConstructor;
        private IMethodSignature _uninitializedConstructorSignature;
        private INamedType _nullableType;
        private IType _uninitializedType;
        private ITypeSignature _typeBindingType;
        private ITypeSignature _ulongType;
        private ITypeSignature _objectViewType;
        private ModuleDeclaration _module;
        private TypeDefDeclaration _starcounterImplementationTypeDef;
        //private MethodDefDeclaration _lucentClientAssemblyInitializerMethod;
        private WeavingHelper _weavingHelper;
        private bool _weaveForIPC;
        private AssemblyRefDeclaration _starcounterAssemblyReference;

        /// <summary>
        /// 
        /// </summary>
        public ScTransformTask() {
            _fieldAccessors = new Dictionary<String, MethodPair>();
            _writer = new InstructionWriter();
            _staticConstructors = new List<MethodDefDeclaration>();
            _fieldAdvices = new List<InsteadOfFieldAccessAdvice>();
            _methodAdvices = new List<IMethodLevelAdvice>();
            _weavedLucentAccessorAdvices = new List<ReimplementWeavedLucentAccessorAdvice>();
        }

        /// <summary>
        /// Gets the field name, but without the assembly name.
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        private static string GetFieldName(IMember field) {
            StringBuilder sb = new StringBuilder();
            field.DeclaringType.WriteReflectionName(sb, ReflectionNameOptions.None);
            sb.Append(".");
            sb.Append(field.Name);
            return sb.ToString();
        }

        /// <summary>
        /// Retrieves the accessors of a field, typically defined in another assembly.
        /// </summary>
        /// <param name="field">Field for which accessors are requested.</param>
        /// <param name="getMethod">Get accessor.</param>
        /// <param name="setMethod">Set accessor.</param>
        private void GetAccessors(IField field,
                                  out IMethod getMethod,
                                  out IMethod setMethod) {
            MethodPair pair;
            MethodSignature signature;
            String fieldName = GetFieldName(field);
            if (!_fieldAccessors.TryGetValue(fieldName, out pair)) {
                // We did not find the field accessors. This maybe because the
                // entity type is in another assembly and this assembly was
                // cached, so the SetAccessors method was not called.
                signature = new MethodSignature(_module,
                                                CallingConvention.HasThis,
                                                field.FieldType,
                                                null,
                                                0);
                getMethod = field.DeclaringType.Methods.GetMethod("get_" + field.Name,
                                                                  signature,
                                                                  BindingOptions.Default);

                signature = new MethodSignature(_module,
                                                CallingConvention.HasThis,
                                                _module.Cache.GetIntrinsic(IntrinsicType.Void),
                                                new[] { field.FieldType },
                                                0);
                setMethod = field.DeclaringType.Methods.GetMethod("set_" + field.Name,
                                                                  signature,
                                                                  BindingOptions.Default);
                pair = new MethodPair {
                    GetMethod = getMethod,
                    SetMethod = setMethod
                };
                _fieldAccessors.Add(GetFieldName(field), pair);
            }
            getMethod = pair.GetMethod;
            setMethod = pair.SetMethod;
        }

        /// <summary>
        /// Used to store a pair of get and set accessors in a dictionary.
        /// </summary>
        private class MethodPair {
            public IMethod GetMethod;
            public IMethod SetMethod;
        }

        /// <summary>
        /// Entry point of the current task.
        /// </summary>
        /// <returns><b>true</b> in case of success, otherwise <b>false</b>.</returns>
        public override bool Execute() {
            DatabaseAttribute databaseAttribute;
            DatabaseClass databaseClass;
            DatabaseExtensionClass databaseExtensionClass;
            DatabaseEntityClass databaseEntityClass;
            DatabaseSocietyClass databaseSocietyClass;
            FieldDefDeclaration field;
            IEnumerator<MetadataDeclaration> typeEnumerator;
            IMethod getMethod;
            IMethod setMethod;
            MethodDefDeclaration constructor;
            MethodDefDeclaration staticConstructor;
            ScAnalysisTask analysisTask;
            TypeDefDeclaration typeDef;
            TypeRefDeclaration typeRef;
            IMethod weavedAssemblyAttributeCtor;

            _module = this.Project.Module;
            analysisTask = ScAnalysisTask.GetTask(this.Project);

            _starcounterAssemblyReference = FindStarcounterAssembly();
            if (_starcounterAssemblyReference == null) {
                // No reference to Starcounter. We don't need to transform anything.
                // Lets skip the rest of the code.

                ScMessageSource.Instance.Write(
                    SeverityType.Info,
                    "SCINF03",
                    new Object[] { _module.Name }
                    );
                return true;
            }

            // Check if the transformation kind has been established to be None,
            // meaning we need not to transform at all.

            if (analysisTask.TransformationKind == WeaverTransformationKind.None) {
                ScMessageSource.Instance.Write(
                    SeverityType.Info,
                    "SCINF04",
                    new Object[] { _module.Name }
                    );

                // Disable all upcoming tasks in this project, since we don't
                // need to do anything with this assembly

                this.Project.Tasks["ScTransactionScope"].Disabled = true;

                // "Connection bound objects" is currently not enabled. See the
                // P4 changelist for this commenting to see some more information
                // on the reason and status of this.
                // this.Project.Tasks["ScConnectionBoundObject"].Disabled = true;

                this.Project.Tasks["ScEnhanceThreadingTask"].Disabled = true;
                this.Project.Tasks["Compile"].Disabled = true;

                return true;
            }

            // Some kind of transformation is needed. Lets continue.
            //
            // If it isn't tagged as pre-weaved, we should either weave it normally, or we
            // should weave it for external use.

            _weaveForIPC = analysisTask.TransformationKind == WeaverTransformationKind.UserCodeToIPC;

            // Do initialization needed for all kinds of transformation.
            // User code weavers requires extra initialization, invoked
            // further down.

            InitializeForAllTransformationKinds();

            // If the assembly indicates it has already been weaved, we assume we are in
            // the context of the database, with the mission to readapt it.

            if (analysisTask.TransformationKind == WeaverTransformationKind.IPCToDatabase) {
                return ExecuteOnIPCWeavedAssembly(analysisTask);
            }

            //
            // We will weave it. Make sure we redirect it first and then mark it
            // as weaved for IPC if it's such a context we are weaving for.

            ScMessageSource.Instance.Write(
                SeverityType.ImportantInfo, "SCINF02",
                new Object[] { _module.Name }
                );

            // Initialize extra for user code weavers.

            InitializeForUserCodeWeavers();

            if (_weaveForIPC) {
                weavedAssemblyAttributeCtor = _module.FindMethod(
                    typeof(AssemblyWeavedForIPCAttribute).GetConstructor(Type.EmptyTypes),
                    BindingOptions.Default
                    );
                _module.AssemblyManifest.CustomAttributes.Add(new CustomAttributeDeclaration(weavedAssemblyAttributeCtor));
            }

            // Process database classes defined in the current assembly.

            foreach (DatabaseClass dbc in analysisTask.DatabaseClassesInCurrentModule) {
                ScTransformTrace.Instance.WriteLine("Transforming {0}.", dbc);
                typeDef = (TypeDefDeclaration)_module.FindType(dbc.Name, BindingOptions.OnlyExisting);

                // Generate field accessors and add corresponding advices
                foreach (DatabaseAttribute dba in dbc.Attributes) {
                    if (dba.IsField && dba.IsPersistent) {
                        field = typeDef.Fields.GetByName(dba.Name);
                        GenerateFieldAccessors(dba, field);
                    }
                }

                // Index static constructors.
                staticConstructor = typeDef.Methods.GetOneByName(".cctor");
                if (staticConstructor != null) {
                    _staticConstructors.Add(staticConstructor);
                }

                // Transformations specific to entity classes.
                databaseEntityClass = dbc as DatabaseEntityClass;
                if (databaseEntityClass != null) {
                    databaseSocietyClass = databaseEntityClass as DatabaseSocietyClass;
                    if (databaseSocietyClass == null) {
                        AddTypeReferenceFields(typeDef);
                    } else {
                        AddKindReferenceField(typeDef, databaseSocietyClass);
                    }
                }
            }

            // Re-iterate all database classes in the current module and process
            // each constructor.

            foreach (DatabaseClass dbc in analysisTask.DatabaseClassesInCurrentModule) {
                typeDef = (TypeDefDeclaration)_module.FindType(dbc.Name, BindingOptions.OnlyExisting);
                EnhanceConstructors(typeDef, dbc);
            }

            // Look at imported types, fields and methods and add corresponding advices

            typeEnumerator = _module.GetDeclarationEnumerator(TokenType.TypeRef);
            while (typeEnumerator.MoveNext()) {
                typeRef = (TypeRefDeclaration)typeEnumerator.Current;
                databaseClass = ScAnalysisTask.DatabaseSchema.FindDatabaseClass
                                                    (
                                                        ScAnalysisTask.GetTypeReflectionName(typeRef)
                                                    );
                if (databaseClass != null) {
                    // It's a database class. We have to weave access to fields.
                    foreach (FieldRefDeclaration fieldRef in typeRef.FieldRefs) {
                        if (databaseClass.Attributes.Contains(fieldRef.Name)) {
                            databaseAttribute = databaseClass.Attributes[fieldRef.Name];
                            if (databaseAttribute.IsPersistent) {
                                GetAccessors(fieldRef, out getMethod, out setMethod);
                                getMethod = (IMethod)getMethod.Translate(this._module);
                                if (setMethod != null) {
                                    setMethod = (IMethod)setMethod.Translate(this._module);
                                }
                                _fieldAdvices.Add(new InsteadOfFieldAccessAdvice(fieldRef,
                                                                                 getMethod,
                                                                                 setMethod));
                            }
                        }
                    }
                }
            }

            // Enhance anonymous types

            typeEnumerator = _module.GetDeclarationEnumerator(TokenType.TypeDef);
            while (typeEnumerator.MoveNext()) {
                typeDef = (TypeDefDeclaration)typeEnumerator.Current;
                if (IsAnonymousType(typeDef)) {
                    EnhanceAnonymousType(typeDef);
                }
            }

            // We have to index usages a second time, because we have changed method implementations.

            IndexUsagesTask.Execute(this.Project);
            return true;
        }

        private bool ExecuteOnIPCWeavedAssembly(ScAnalysisTask analysisTask) {
            TypeDefDeclaration typeDef;
            string attributeIndexVariableName;
            FieldDefDeclaration attributeIndexField;
            RemoveTask removeTask;
            IType weavedAssemblyAttributeType;
            CustomAttributeDeclaration weavedAttribute;

            weavedAssemblyAttributeType = FindStarcounterType(typeof(AssemblyWeavedForIPCAttribute));
            weavedAttribute =
                _module.AssemblyManifest.CustomAttributes.GetOneByType(weavedAssemblyAttributeType);

            removeTask = RemoveTask.GetTask(this.Project);
            removeTask.MarkForRemoval(weavedAttribute);

            foreach (DatabaseClass dbc in analysisTask.DatabaseClassesInCurrentModule) {
                ScTransformTrace.Instance.WriteLine("Retransforming {0}.", dbc);

                typeDef = (TypeDefDeclaration)_module.FindType(dbc.Name, BindingOptions.OnlyExisting);

                // Iterate each attribute. It will reference the IPC-weaved property.
                // Get it and get getter and, possibly, setter.

                foreach (DatabaseAttribute dba in dbc.Attributes) {
                    if (dba.IsField && dba.IsPersistent) {
                        // Reimplement pre-weaved accessors

                        PropertyDeclaration prop = dba.GetPropertyDefinition();
                        _weavedLucentAccessorAdvices.Add(new ReimplementWeavedLucentAccessorAdvice(_dbStateMethodProvider, prop, dba.Index));

                        // Remove the corresponding static attribute index field

                        attributeIndexVariableName = WeaverNamingConventions.MakeAttributeIndexVariableName(prop.Name);
                        attributeIndexField = typeDef.Fields.GetByName(attributeIndexVariableName);

                        removeTask.MarkForRemoval(attributeIndexField);
                    }
                }
            }

            return true;
        }

        private void InitializeForAllTransformationKinds() {
            string dynamicLibDir;

            // Only consider using the dynamic / code generated library if we are
            // weaving inside the database, not when weaving targetting an IPC-ish
            // context.

            dynamicLibDir = _weaveForIPC
                ? null
                : Project.Properties["ScDynamicLibInputDir"];

            _dbStateMethodProvider = new DbStateMethodProvider(_module, dynamicLibDir);
            _castHelper = new CastHelper(_module);
        }

        private void InitializeForUserCodeWeavers() {
            this.Initialize();
        }

        /// <summary>
        /// Initializes and sets all fields needed for user code weavers.
        /// </summary>
        private new void Initialize() {
            ConstructorInfo cstrInfo;
            IntrinsicTypeSignature voidTypeSign;
            ITypeSignature[] uninitTypeSignArr;
            Type type;

            voidTypeSign = _module.Cache.GetIntrinsic(IntrinsicType.Void);

            _uninitializedType = (IType)_module.Cache.GetType(typeof(Uninitialized));

            uninitTypeSignArr = new ITypeSignature[] { _uninitializedType };
            _uninitializedConstructorSignature = new MethodSignature(_module,
                                                                     CallingConvention.HasThis,
                                                                     voidTypeSign,
                                                                     uninitTypeSignArr,
                                                                     0);
            _nullableType = (INamedType)_module.FindType(
                typeof(Nullable<>),
                BindingOptions.RequireGenericDefinition
                );

            _typeBindingType = _module.Cache.GetType(typeof(Sc.Server.Binding.TypeBinding));
            _ulongType = _module.Cache.GetIntrinsic(IntrinsicType.UInt64);

            _objectViewType = _module.FindType(typeof(IObjectView), BindingOptions.Default);
            _objectConstructor = _module.FindMethod(typeof(Object).GetConstructor(Type.EmptyTypes),
                                                    BindingOptions.Default);

            type = typeof(AnonymousTypePropertyAttribute);
            cstrInfo = type.GetConstructor(new[] { typeof(int) });
            _objViewPropIndexAttrConstructor = _module.FindMethod(cstrInfo,
                                                                              BindingOptions.Default);

            type = typeof(AnonymousTypeAdapter);
            _adapterGetPropertyMethod = _module.FindMethod(type.GetMethod("GetProperty"),
                                                           BindingOptions.Default);
            _adapterResolveIndexMethod = _module.FindMethod(type.GetMethod("ResolveIndex"),
                                                            BindingOptions.Default);

            _weavingHelper = new WeavingHelper(_module);

            _starcounterImplementationTypeDef = new TypeDefDeclaration {
                Name = WeaverNamingConventions.ImplementationDetailsTypeName,
                Attributes = TypeAttributes.AutoLayout | TypeAttributes.Public | TypeAttributes.Sealed
            };

            _module.Types.Add(_starcounterImplementationTypeDef);

            //if (_weaveForIPC)
            //{
            //    // If we weave for IPC, we make sure the implementation type we hide
            //    // inside each assembly contains a client side initializer routine that
            //    // can set up the runtime environment.

            //    _lucentClientAssemblyInitializerMethod = new MethodDefDeclaration()
            //    {
            //        Name = WeaverNamingConventions.LucentObjectsClientAssemblyInitializerName,
            //        Attributes = MethodAttributes.Static | MethodAttributes.Public,
            //        CallingConvention = CallingConvention.Default
            //    };
            //    _starcounterImplementationTypeDef.Methods.Add(_lucentClientAssemblyInitializerMethod);
            //    _weavingHelper.AddCompilerGeneratedAttribute(_lucentClientAssemblyInitializerMethod.CustomAttributes);

            //    _lucentClientAssemblyInitializerMethod.MethodBody.RootInstructionBlock = _lucentClientAssemblyInitializerMethod.MethodBody.CreateInstructionBlock();

            //    var sequence = _lucentClientAssemblyInitializerMethod.MethodBody.CreateInstructionSequence();
            //    _lucentClientAssemblyInitializerMethod.MethodBody.RootInstructionBlock.AddInstructionSequence(
            //        sequence,
            //        NodePosition.After,
            //        null
            //        );
            //}

        }

        /// <summary>
        /// Searching for the Starcounter assembly reference from the current modules references.
        /// </summary>
        /// <returns>
        /// The assembly reference, or null if not found.
        /// </returns>
        private AssemblyRefDeclaration FindStarcounterAssembly() {
            AssemblyRefDeclaration scAssemblyRef = null;
            StringComparison strComp = StringComparison.InvariantCultureIgnoreCase;
            String assFailedStr;

            foreach (AssemblyRefDeclaration assemblyRef in _module.AssemblyRefs) {
                if (String.Equals(assemblyRef.Name, "Starcounter", strComp)) {
                    if (scAssemblyRef != null) {
                        assFailedStr = "Assembly {0} has more than one reference to Starcounter.dll";
                        throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, (String.Format(assFailedStr, _module.Name)));
                    }
                    scAssemblyRef = assemblyRef;
                }
            }

            return scAssemblyRef;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private IType FindStarcounterType(Type type) {
            if (_starcounterAssemblyReference == null) {
                throw new InvalidOperationException();
            }

            return (IType)_starcounterAssemblyReference.FindType(
                type.FullName,
                BindingOptions.RequireGenericDefinition
                );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeDef"></param>
        private void EnhanceAnonymousType(TypeDefDeclaration typeDef) {
            // TODO: This method really needs to be refactored and broken down into 
            // smaller pieces. As it is now it's really hard to get an overview of all the code.

            Boolean getValueFromNullable;
            Boolean requiresCast;
            CustomAttributeDeclaration attribute;
            FieldDefDeclaration adapterField;
            FieldDefDeclaration objectViewField;
            GenericParameterTypeSignature gptTypeSign;
            GenericTypeInstanceTypeSignature genericInstance;
            IField adapterFieldRef;
            IField objectViewFieldRef;
            IMethod method;
            IMethod nullableGetValueMethod;
            INamedType namedType;
            Int32 index;
            InstructionSequence instructionSequence;
            InstructionSequence oldFirstSequence;
            IntrinsicTypeSignature databaseValueIntrinsicType;
            IntrinsicTypeSignature voidTypeSign;
            InstructionBlock rootInstrBlock;
            ITypeSignature databaseValueType;
            ITypeSignature targetValueType;
            MethodDefDeclaration constructorDef;
            MethodDefDeclaration getterMethodDef;
            MethodDefDeclaration getObjectViewMethod;
            MethodInfo getMethodInfo;
            MethodSignature methodSign;
            ParameterDeclaration paramDecl;
            SerializedValue serializedValue;
            String methodName;
            Type anonymousType;
            TypeSpecDeclaration sourceValueType;

            voidTypeSign = _module.Cache.GetIntrinsic(IntrinsicType.Void);

            // Create new  fields.
            objectViewField = new FieldDefDeclaration() {
                Name = "objectView",
                Attributes = FieldAttributes.Private | FieldAttributes.InitOnly,
                FieldType = _module.Cache.GetType(typeof(IObjectView))
            };
            typeDef.Fields.Add(objectViewField);
            objectViewFieldRef = GenericHelper.GetFieldCanonicalGenericInstance(objectViewField);

            adapterField = new FieldDefDeclaration() {
                Name = "adapter",
                Attributes =
                FieldAttributes.Private | FieldAttributes.InitOnly,
                FieldType =
                this._module.Cache.GetType(typeof(AnonymousTypeAdapter))
            };
            typeDef.Fields.Add(adapterField);
            adapterFieldRef = GenericHelper.GetFieldCanonicalGenericInstance(adapterField);

            // Create a new constructor.
            constructorDef = new MethodDefDeclaration() {
                Name = ".ctor",
                Attributes = MethodAttributes.Public
                                | MethodAttributes.SpecialName
                                | MethodAttributes.RTSpecialName,
                CallingConvention = CallingConvention.HasThis
            };
            typeDef.Methods.Add(constructorDef);

            paramDecl = new ParameterDeclaration(0, "objectView", objectViewFieldRef.FieldType);
            constructorDef.Parameters.Add(paramDecl);
            paramDecl = new ParameterDeclaration(1, "adapter", adapterFieldRef.FieldType);
            constructorDef.Parameters.Add(paramDecl);
            paramDecl = new ParameterDeclaration(-1, null, voidTypeSign);
            paramDecl.Attributes = ParameterAttributes.Retval;
            constructorDef.ReturnParameter = paramDecl;
            constructorDef.MethodBody.RootInstructionBlock
                        = constructorDef.MethodBody.CreateInstructionBlock();

            instructionSequence = constructorDef.MethodBody.CreateInstructionSequence();
            constructorDef.MethodBody.RootInstructionBlock.AddInstructionSequence(instructionSequence,
                                                                                  NodePosition.After,
                                                                                  null);
            _writer.AttachInstructionSequence(instructionSequence);
            _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
            _writer.EmitInstructionMethod(OpCodeNumber.Call, this._objectConstructor);
            _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
            _writer.EmitInstruction(OpCodeNumber.Ldarg_1);
            _writer.EmitInstructionField(OpCodeNumber.Stfld, objectViewFieldRef);
            _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
            _writer.EmitInstruction(OpCodeNumber.Ldarg_2);
            _writer.EmitInstructionField(OpCodeNumber.Stfld, adapterFieldRef);
            _writer.EmitInstruction(OpCodeNumber.Ret);
            _writer.DetachInstructionSequence();

            // Enhance every property setter.
            index = 0;
            foreach (PropertyDeclaration property in typeDef.Properties) {
                if (property.Members.Contains(MethodSemantics.Getter)) {
                    // Add a custom attribute with the index.

                    serializedValue = IntrinsicSerializationType.CreateValue(_module, index);
                    attribute = new CustomAttributeDeclaration(_objViewPropIndexAttrConstructor);
                    attribute.ConstructorArguments.Add(new MemberValuePair(MemberKind.Parameter,
                                                                           0,
                                                                           "0",
                                                                           serializedValue));
                    property.CustomAttributes.Add(attribute);

                    // Enhance the property getter
                    getterMethodDef = property.Members.GetBySemantic(MethodSemantics.Getter).Method;

                    // Get the first sequence.
                    oldFirstSequence =
                        getterMethodDef.MethodBody.RootInstructionBlock.FindFirstInstructionSequence();

                    // Put a new sequence before the first sequence.
                    instructionSequence = getterMethodDef.MethodBody.CreateInstructionSequence();
                    oldFirstSequence.ParentInstructionBlock.AddInstructionSequence(instructionSequence,
                                                                                   NodePosition.Before,
                                                                                   oldFirstSequence);
                    _writer.AttachInstructionSequence(instructionSequence);
                    _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                    _writer.EmitInstructionField(OpCodeNumber.Ldfld, objectViewFieldRef);
                    _writer.EmitBranchingInstruction(OpCodeNumber.Brfalse, oldFirstSequence);

                    // Determine the value type.
                    targetValueType = getterMethodDef.ReturnParameter.ParameterType;
                    requiresCast = false;
                    genericInstance = targetValueType.GetNakedType(TypeNakingOptions.None)
                                                        as GenericTypeInstanceTypeSignature;
                    if (genericInstance != null) {
                        if (TypeComparer.GetInstance().Equals(genericInstance.GenericDefinition,
                                                              _nullableType)) {
                            databaseValueType = genericInstance.GenericArguments[0];
                            getValueFromNullable = true;
                        } else {
                            databaseValueType = genericInstance;
                            getValueFromNullable = false;
                        }
                    } else {
                        databaseValueType = targetValueType;
                        getValueFromNullable = false;
                    }

                    databaseValueIntrinsicType = databaseValueType as IntrinsicTypeSignature;
                    if (databaseValueIntrinsicType != null) {
                        switch (databaseValueIntrinsicType.IntrinsicType) {
                            case IntrinsicType.Boolean:
                            case IntrinsicType.Byte:
                            case IntrinsicType.Double:
                            case IntrinsicType.Int16:
                            case IntrinsicType.Int32:
                            case IntrinsicType.Int64:
                            case IntrinsicType.SByte:
                            case IntrinsicType.Single:
                            case IntrinsicType.String:
                            case IntrinsicType.UInt16:
                            case IntrinsicType.UInt32:
                            case IntrinsicType.UInt64:
                                methodName = "Get"
                                             + databaseValueIntrinsicType.IntrinsicType.ToString();
                                break;
                            default:
                                methodName = null;
                                break;
                        }
                    } else {
                        namedType = databaseValueType as INamedType;
                        if (namedType != null) {
                            switch (namedType.Name) {
                                case "System.Decimal":
                                    methodName = "GetDecimal";
                                    break;
                                case "System.DateTime":
                                    methodName = "GetDateTime";
                                    break;
                                case "Starcounter.Binary":
                                    methodName = "GetBinary";
                                    break;
                                default:
                                    methodName = null;
                                    break;
                            }
                        } else {
                            methodName = null;
                        }

                        // If the type implements IObjectView, we use IObjectView.GetObject
                        if (methodName == null && databaseValueType.IsAssignableTo(_objectViewType)) {
                            methodName = "GetObject";
                            requiresCast = true;
                        }
                    }

                    if (methodName != null) {
                        method = _module.FindMethod(typeof(IObjectView).GetMethod(methodName),
                                                         BindingOptions.Default);
                        _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                        _writer.EmitInstructionField(OpCodeNumber.Ldfld, objectViewFieldRef);
                        _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                        _writer.EmitInstructionField(OpCodeNumber.Ldfld, objectViewFieldRef);
                        _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                        _writer.EmitInstructionField(OpCodeNumber.Ldfld, adapterFieldRef);
                        _writer.EmitInstructionInt32(OpCodeNumber.Ldc_I4, index);
                        _writer.EmitInstructionMethod(OpCodeNumber.Call, _adapterResolveIndexMethod);
                        _writer.EmitInstructionMethod(OpCodeNumber.Callvirt, method);

                        // If the source is a nullable and the target is not
                        // we should get the value out of the nullable.
                        if (getValueFromNullable) {
                            genericInstance
                                = new GenericTypeInstanceTypeSignature(_nullableType,
                                                                       new[] { databaseValueType });
                            sourceValueType = _module.TypeSpecs.GetBySignature(genericInstance);

                            gptTypeSign = GenericParameterTypeSignature.GetInstance(
                                                                            _module,
                                                                            0,
                                                                            GenericParameterKind.Type
                                                                        );
                            methodSign = new MethodSignature(_module,
                                                             CallingConvention.HasThis,
                                                             gptTypeSign,
                                                             null,
                                                             0);
                            nullableGetValueMethod
                                = sourceValueType.MethodRefs.GetMethod(
                                                                "get_Value",
                                                                methodSign,
                                                                BindingOptions.Default
                                                             );
                            _writer.EmitInstructionMethod(OpCodeNumber.Call, nullableGetValueMethod);
                        }

                        if (requiresCast) {
                            _writer.EmitInstructionType(OpCodeNumber.Castclass, targetValueType);
                        }
                    } else {
                        // We cannot find, at build-time, a way to get the property from the object 
                        // view and to convert it into what we need. We will give the runtime a chance 
                        // to do it.
                        _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                        _writer.EmitInstructionField(OpCodeNumber.Ldfld, adapterFieldRef);
                        _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                        _writer.EmitInstructionField(OpCodeNumber.Ldfld, objectViewFieldRef);
                        _writer.EmitInstructionInt32(OpCodeNumber.Ldc_I4, index);
                        _weavingHelper.GetRuntimeType(databaseValueType, _writer);
                        _writer.EmitInstructionMethod(OpCodeNumber.Call, _adapterGetPropertyMethod);
                        _weavingHelper.FromObject(targetValueType, _writer);
                    }
                    _writer.EmitInstruction(OpCodeNumber.Ret);
                    _writer.DetachInstructionSequence();

                    index++;
                }
            }

            // Implement the IAnonymousState interface.
            anonymousType = typeof(IAnonymousType);
            typeDef.InterfaceImplementations.Add(
                                                _module.FindType(anonymousType,
                                                                 BindingOptions.Default)
                                             );

            getObjectViewMethod = new MethodDefDeclaration() {
                Name = "IAnonymousType.get_UnderlyingObject",
                Attributes = MethodAttributes.Virtual
                                | MethodAttributes.Private
                                | MethodAttributes.ReuseSlot,
                CallingConvention = CallingConvention.HasThis
            };
            typeDef.Methods.Add(getObjectViewMethod);

            paramDecl = new ParameterDeclaration(-1, null, this._objectViewType);
            paramDecl.Attributes = ParameterAttributes.Retval;
            getObjectViewMethod.ReturnParameter = paramDecl;

            getMethodInfo = anonymousType.GetProperty("UnderlyingObject").GetGetMethod();
            getObjectViewMethod.InterfaceImplementations.Add(
                                                            _module.FindMethod(getMethodInfo,
                                                            BindingOptions.Default)
                                                        );

            rootInstrBlock = getObjectViewMethod.MethodBody.CreateInstructionBlock();
            getObjectViewMethod.MethodBody.RootInstructionBlock = rootInstrBlock;
            instructionSequence = getObjectViewMethod.MethodBody.CreateInstructionSequence();
            rootInstrBlock.AddInstructionSequence(instructionSequence, NodePosition.After, null);
            _writer.AttachInstructionSequence(instructionSequence);
            _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
            _writer.EmitInstructionField(OpCodeNumber.Ldfld, objectViewFieldRef);
            _writer.EmitInstruction(OpCodeNumber.Ret);
            _writer.DetachInstructionSequence();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeDef"></param>
        /// <returns></returns>
        private static Boolean IsAnonymousType(TypeDefDeclaration typeDef) {
            return typeDef.Name.StartsWith("<>f__AnonymousType")
                    || typeDef.Name.StartsWith("VB$AnonymousType");
        }

        /// <summary>
        /// Adds a kind reference field to a society object.
        /// </summary>
        /// <param name="declaringTypeDef">
        /// <see cref="TypeDefDeclaration"/> of the society object.
        /// </param>
        /// <param name="databaseSocietyClass">
        /// The society object.
        /// </param>
        private void AddKindReferenceField(
            TypeDefDeclaration declaringTypeDef,
            DatabaseSocietyClass databaseSocietyClass) {
            // The SO binding layer must be moved out of the database. It
            // is currently not supported. To see the old implementation,
            // consult the perforce history.

            throw new NotImplementedException();
        }

        /// <summary>
        /// Decorates the assembly level implementation type with the neccessary
        /// infrastructure reference fields for the given type.
        /// </summary>
        /// <param name="typeDef">The type to decorate.</param>
        private void AddTypeReferenceFields(TypeDefDeclaration typeDef) {
            FieldDefDeclaration typeAddress;
            FieldDefDeclaration typeBinding;
            FieldDefDeclaration typeReference;

            typeAddress = new FieldDefDeclaration {
                Name = WeaverNamingConventions.GetTypeAddressFieldName(typeDef),
                Attributes = (FieldAttributes.Assembly | FieldAttributes.Static),
                FieldType = _ulongType
            };

            typeBinding = new FieldDefDeclaration {
                Name = WeaverNamingConventions.GetTypeBindingFieldName(typeDef),
                Attributes = (FieldAttributes.Assembly | FieldAttributes.Static),
                FieldType = _typeBindingType
            };

            // Add also a field referencing the type itself and name it after
            // the full name. This way, we can speed up the client side initialization
            // by using the implementation type as an index for all types that are
            // entities (and needs initialization).

            typeReference = new FieldDefDeclaration {
                Name = WeaverNamingConventions.GetTypeFieldName(typeDef),
                Attributes = (FieldAttributes.Assembly | FieldAttributes.Static),
                FieldType = typeDef
            };

            _starcounterImplementationTypeDef.Fields.Add(typeAddress);
            _starcounterImplementationTypeDef.Fields.Add(typeBinding);
            _starcounterImplementationTypeDef.Fields.Add(typeReference);
        }

        /// <summary>
        /// Method called by the code weaver when it wants to know which advices 
        /// we want to provide.
        /// </summary>
        /// <param name="codeWeaver">
        /// The code weaver to which we should provide advices.
        /// </param>
        public void ProvideAdvices(PostSharp.Sdk.CodeWeaver.Weaver codeWeaver) {
            Singleton<MetadataDeclaration> metaSingleton;
            StaticConstructorAdvice staticConstructorAdvice;

            foreach (InsteadOfFieldAccessAdvice advice in _fieldAdvices) {
                metaSingleton = new Singleton<MetadataDeclaration>((MetadataDeclaration)advice.Field);
                codeWeaver.AddMethodLevelAdvice(advice,
                                                null,
                                                JoinPointKinds.InsteadOfGetField
                                                    | JoinPointKinds.InsteadOfSetField
                                                    | JoinPointKinds.InsteadOfGetFieldAddress,
                                                metaSingleton);
            }

            foreach (ReimplementWeavedLucentAccessorAdvice advice in _weavedLucentAccessorAdvices) {
                codeWeaver.AddMethodLevelAdvice(
                    advice,
                    new Singleton<MethodDefDeclaration>(advice.AccessorProperty.Getter),
                    JoinPointKinds.InsteadOfGetField | JoinPointKinds.InsteadOfCall,
                    null);

                if (advice.AccessorProperty.Setter != null) {
                    codeWeaver.AddMethodLevelAdvice(
                        advice,
                        new Singleton<MethodDefDeclaration>(advice.AccessorProperty.Setter),
                        JoinPointKinds.InsteadOfGetField | JoinPointKinds.InsteadOfCall,
                        null);
                }
            }

            staticConstructorAdvice = new StaticConstructorAdvice(_module);
            foreach (MethodDefDeclaration staticConstructor in _staticConstructors) {

                codeWeaver.AddMethodLevelAdvice(staticConstructorAdvice,
                                                new Singleton<MethodDefDeclaration>(staticConstructor),
                                                JoinPointKinds.BeforeMethodBody
                                                    | JoinPointKinds.AfterMethodBodyAlways,
                                                null);
            }

            foreach (IMethodLevelAdvice advice in _methodAdvices) {
                codeWeaver.AddMethodLevelAdvice(advice,
                                                advice.TargetMethods,
                                                advice.JoinPointKinds,
                                                advice.Operands);
            }

            // If a initializer has been defined, make sure we attach an advice that will
            // implement it.

            if (_weaveForIPC) {
                //LucentAssemblyInitializerMethodAdvice moduleInitializerAdvice = new LucentAssemblyInitializerMethodAdvice();

                //codeWeaver.AddMethodLevelAdvice(
                //    moduleInitializerAdvice,
                //    new Singleton<MethodDefDeclaration>(_lucentClientAssemblyInitializerMethod),
                //    JoinPointKinds.BeforeMethodBody,
                //    null
                //    );

                LucentClassInitializerMethodAdvice pcia = new LucentClassInitializerMethodAdvice();

                foreach (DatabaseClass dbc in ScAnalysisTask.GetTask(this.Project).DatabaseClassesInCurrentModule) {
                    var typeDef = (TypeDefDeclaration)_module.FindType(dbc.Name, BindingOptions.OnlyExisting);
                    codeWeaver.AddTypeLevelAdvice(
                        pcia,
                        JoinPointKinds.BeforeStaticConstructor,
                        new Singleton<TypeDefDeclaration>(typeDef)
                        );
                }
            }
        }

        /// <summary>
        /// Generates the <b>get</b> and <b>set</b> accessors for a field, generate a property,
        /// and add an advice to replace field accesses.
        /// </summary>
        /// <param name="databaseAttribute">
        /// The <see cref="DatabaseAttribute"/> for which accessors have to be generated.
        /// </param>
        /// <param name="field">
        /// The <see cref="FieldDefDeclaration"/> corresponding 
        /// to <paramref name="databaseAttribute"/>.
        /// </param>
        private void GenerateFieldAccessors(DatabaseAttribute databaseAttribute,
                                            FieldDefDeclaration field) {
            // TODO: This method really needs to be refactored and broken down into 
            // smaller pieces. As it is now it's really hard to get an overview of all the code.

            Boolean haveSetMethod;
            CallingConvention callingConvention;
            DatabaseAttribute realDatabaseAttribute;
            IMethod dbStateMethod;
            InstructionSequence sequence;
            ITypeSignature dbStateCast;
            MethodAttributes methodAttributes;
            MethodDefDeclaration getMethod;
            MethodDefDeclaration setMethod;
            MethodSemanticDeclaration getSemantic;
            MethodSemanticDeclaration setSemantic;
            ParameterDeclaration valueParameter;
            PropertyDeclaration property;
            FieldDefDeclaration attributeIndexField;

            ScTransformTrace.Instance.WriteLine("Generating accessors for {0}.", databaseAttribute);

            attributeIndexField = null;

            if (_weaveForIPC) {
                // Generate a static field that can hold the attribute index in
                // hosted environments.

                attributeIndexField = new FieldDefDeclaration {
                    Name = WeaverNamingConventions.MakeAttributeIndexVariableName(field.Name),
                    Attributes = (FieldAttributes.Private | FieldAttributes.Static),
                    FieldType = _module.Cache.GetIntrinsic(IntrinsicType.Int32)
                };
                field.DeclaringType.Fields.Add(attributeIndexField);
                _weavingHelper.AddCompilerGeneratedAttribute(attributeIndexField.CustomAttributes);
            }

            // Compute method attributes according to field attributes.
            switch (field.Attributes & FieldAttributes.FieldAccessMask) {
                case FieldAttributes.Assembly:
                    methodAttributes = MethodAttributes.Assembly;
                    break;
                case FieldAttributes.FamANDAssem:
                    methodAttributes = MethodAttributes.FamANDAssem;
                    break;
                case FieldAttributes.Family:
                    methodAttributes = MethodAttributes.Family;
                    break;
                case FieldAttributes.FamORAssem:
                    methodAttributes = MethodAttributes.FamORAssem;
                    break;
                case FieldAttributes.Private:
                    methodAttributes = MethodAttributes.Private;
                    break;
                case FieldAttributes.Public:
                    methodAttributes = MethodAttributes.Public;
                    break;
                default:
                    throw new Exception();
            }
            methodAttributes |= MethodAttributes.HideBySig;

            if ((field.Attributes & FieldAttributes.Static) != 0) {
                methodAttributes |= MethodAttributes.Static;
                callingConvention = CallingConvention.Default;
            } else {
                callingConvention = CallingConvention.HasThis;
            }

            realDatabaseAttribute = databaseAttribute.SynonymousTo ?? databaseAttribute;

            // Generate the Get accessor.
            _dbStateMethodProvider.GetGetMethod(field.FieldType,
                                                realDatabaseAttribute,
                                                out dbStateMethod,
                                                out dbStateCast);

            getMethod = new MethodDefDeclaration() {
                Name = ("get_" + field.Name),
                Attributes = methodAttributes,
                CallingConvention = callingConvention
            };

            field.DeclaringType.Methods.Add(getMethod);
            getMethod.CustomAttributes.Add(_weavingHelper.GetDebuggerNonUserCodeAttribute());
            getMethod.ReturnParameter = new ParameterDeclaration {
                Attributes = ParameterAttributes.Retval,
                ParameterType = field.FieldType
            };
            getMethod.MethodBody.RootInstructionBlock = getMethod.MethodBody.CreateInstructionBlock();

            sequence = getMethod.MethodBody.CreateInstructionSequence();
            getMethod.MethodBody.RootInstructionBlock.AddInstructionSequence(sequence,
                                                                             NodePosition.After,
                                                                             null);
            _writer.AttachInstructionSequence(sequence);

            // We will call a method with this stack transition:
            // <this>, <index> -> <value>
            _writer.EmitInstruction(OpCodeNumber.Ldarg_0);

            if (_weaveForIPC) {
                _writer.EmitInstructionField(OpCodeNumber.Ldsfld, attributeIndexField);
            } else {
                _writer.EmitInstructionInt32(OpCodeNumber.Ldc_I4, databaseAttribute.Index);
            }

            if (dbStateCast == null) {
                // If we don't have to cast the value, we can use a 'tail' call.
                _writer.EmitPrefix(InstructionPrefixes.Tail);
            }
            _writer.EmitInstructionMethod(OpCodeNumber.Call, dbStateMethod);

            // We have the field value on the stack, but we may need to cast it.
            if (dbStateCast != null) {
                if (!_castHelper.EmitCast(dbStateCast, field.FieldType, _writer, ref sequence)) {
                    // Not supported (yet).
                    throw new NotSupportedException(String.Format("Don't know how to cast {0} to {1}.",
                                                                  dbStateCast,
                                                                  field.FieldType));
                }
            }
            _writer.EmitInstruction(OpCodeNumber.Ret);
            _writer.DetachInstructionSequence();

            // Generate the Set accessor.
            haveSetMethod = _dbStateMethodProvider.GetSetMethod(field.FieldType,
                                                                realDatabaseAttribute,
                                                                out dbStateMethod,
                                                                out dbStateCast);
            if (haveSetMethod) {
                setMethod = new MethodDefDeclaration() {
                    Name = ("set_" + field.Name),
                    Attributes = methodAttributes,
                    CallingConvention = callingConvention
                };
                field.DeclaringType.Methods.Add(setMethod);
                setMethod.CustomAttributes.Add(_weavingHelper.GetDebuggerNonUserCodeAttribute());
                setMethod.ReturnParameter = new ParameterDeclaration {
                    Attributes = ParameterAttributes.Retval,
                    ParameterType = _module.Cache.GetIntrinsic(IntrinsicType.Void)
                };

                valueParameter = new ParameterDeclaration(0, "value", field.FieldType);
                setMethod.Parameters.Add(valueParameter);
                setMethod.MethodBody.RootInstructionBlock
                                        = setMethod.MethodBody.CreateInstructionBlock();
                sequence = setMethod.MethodBody.CreateInstructionSequence();
                setMethod.MethodBody.RootInstructionBlock.AddInstructionSequence(sequence,
                                                                                 NodePosition.After,
                                                                                 null);
                _writer.AttachInstructionSequence(sequence);

                // We need to prepare this stack transition:
                // <this>, <index>, <casted_value> -> void
                _writer.EmitInstruction(OpCodeNumber.Ldarg_0);

                if (_weaveForIPC) {
                    _writer.EmitInstructionField(OpCodeNumber.Ldsfld, attributeIndexField);
                } else {
                    _writer.EmitInstructionInt32(OpCodeNumber.Ldc_I4, databaseAttribute.Index);
                }

                _writer.EmitInstructionParameter(OpCodeNumber.Ldarg, valueParameter);

                // We have the field value on the stack, but we may need to cast it.
                if (dbStateCast != null) {
                    if (!_castHelper.EmitCast(field.FieldType, dbStateCast, _writer, ref sequence)) {
                        throw new NotSupportedException(string.Format("Don't know how to cast {0} to {1}.",
                                                                      field.FieldType,
                                                                      dbStateCast));
                    }
                }

                // We can make a tail call since it is the last instruction in the method.
                _writer.EmitPrefix(InstructionPrefixes.Tail);
                _writer.EmitInstructionMethod(OpCodeNumber.Call, dbStateMethod);
                _writer.EmitInstruction(OpCodeNumber.Ret);
                _writer.DetachInstructionSequence();
            } else {
                setMethod = null;
                ScTransformTrace.Instance.WriteLine("This field is read-only. No set accessor generated.");
            }

            // Generate the property
            property = new PropertyDeclaration() {
                PropertyType = field.FieldType,
                Name = field.Name
            };
            field.DeclaringType.Properties.Add(property);
            field.CustomAttributes.MoveContentTo(property.CustomAttributes);

            getSemantic = new MethodSemanticDeclaration() {
                Semantic = MethodSemantics.Getter
            };
            property.Members.Add(getSemantic);
            getSemantic.Method = getMethod;

            if (setMethod != null && !databaseAttribute.IsInitOnly) {
                setSemantic = new MethodSemanticDeclaration() {
                    Semantic = MethodSemantics.Setter
                };
                property.Members.Add(setSemantic);
                setSemantic.Method = setMethod;
            }

            // Mark the field for removal.

            // Don't do this if we are preparing an assembly for an external process.
            // Only do this if we are running in the database.

            //        if (!_weaveForIPC)
            //        {
            RemoveTask.GetTask(Project).MarkForRemoval(field);
            //        }

            _fieldAdvices.Add(new InsteadOfFieldAccessAdvice(field, getMethod, setMethod));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeDef"></param>
        /// <param name="databaseClass"></param>
        private void EnhanceConstructors(TypeDefDeclaration typeDef, DatabaseClass databaseClass) {
            CustomAttributeDeclaration customAttr;
            IMethod baseUninitializedConstructor;
            IMethod replacementConstructor;
            InstructionBlock rootInstrBlock;
            InstructionSequence sequence;
            IType forbiddenType;
            IType parentType;
            MethodBodyDeclaration constructorImplementation;
            MethodDefDeclaration enhancedConstructor;
            MethodDefDeclaration referencedConstructorDef;
            MethodDefDeclaration uninitializedConstructor;
            MethodSignature signature;
            Object tag;
            ParameterDeclaration paramDecl;
            EntityConstructorCallAdvice advice;
            TypeDefDeclaration parentTypeDef;
            FieldDefDeclaration typeAddressField;
            FieldDefDeclaration typeBindingField;

            // Skip if the type has already been processed.
            if (typeDef.GetTag(_constructorEnhancedTagGuid) != null) {
                return;
            }

            // We don't enhance constructors if there is a weaver directive.
            if ((databaseClass.WeaverDirectives
                    & (Int32)WeaverDirectives.ExcludeConstructorTransformation) != 0) {
                ScTransformTrace.Instance.WriteLine(
                            "Don't enhance constructors of {{{0}}} because of weaver directives.",
                            typeDef
                );
                return;
            }

            // Ensure that the base type has been processed.
            parentType = typeDef.BaseType;
            parentTypeDef = parentType as TypeDefDeclaration;

            if (parentTypeDef != null) {
                this.EnhanceConstructors(parentTypeDef, databaseClass.BaseClass);
            }

            ScTransformTrace.Instance.WriteLine("Enhancing constructors of {0}", databaseClass);

            typeDef.SetTag(_constructorEnhancedTagGuid, "");

            // Get infrastucture type reference fields.

            typeAddressField = _starcounterImplementationTypeDef.Fields.GetByName(
                WeaverNamingConventions.GetTypeAddressFieldName(typeDef));
            typeBindingField = _starcounterImplementationTypeDef.Fields.GetByName(
                WeaverNamingConventions.GetTypeBindingFieldName(typeDef));

            // Emit the uninitialized constructor
            ScTransformTrace.Instance.WriteLine("Emitting the uninitialized constructor.");

            // Get the 'uninitialized' constructor on the base type.
            baseUninitializedConstructor = parentType.Methods.GetMethod(
                ".ctor",
                _uninitializedConstructorSignature,
                BindingOptions.Default
                );

            // Define the 'uninitialized' constructor.
            uninitializedConstructor = new MethodDefDeclaration {
                Name = ".ctor",
                Attributes = MethodAttributes.SpecialName
                                | MethodAttributes.RTSpecialName
                                | MethodAttributes.HideBySig
                                | MethodAttributes.Public,
                CallingConvention = CallingConvention.HasThis
            };
            typeDef.Methods.Add(uninitializedConstructor);

            customAttr = _weavingHelper.GetDebuggerNonUserCodeAttribute();
            uninitializedConstructor.CustomAttributes.Add(customAttr);

            paramDecl = new ParameterDeclaration(0, "u", this._uninitializedType);
            uninitializedConstructor.Parameters.Add(paramDecl);

            paramDecl = new ParameterDeclaration {
                Attributes = ParameterAttributes.Retval,
                ParameterType = _module.Cache.GetIntrinsic(IntrinsicType.Void)
            };
            uninitializedConstructor.ReturnParameter = paramDecl;

            rootInstrBlock = uninitializedConstructor.MethodBody.CreateInstructionBlock();
            uninitializedConstructor.MethodBody.RootInstructionBlock = rootInstrBlock;

            sequence = uninitializedConstructor.MethodBody.CreateInstructionSequence();
            rootInstrBlock.AddInstructionSequence(sequence, NodePosition.After, null);
            _writer.AttachInstructionSequence(sequence);
            _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
            _writer.EmitInstruction(OpCodeNumber.Ldnull);
            _writer.EmitInstructionMethod(OpCodeNumber.Call, baseUninitializedConstructor);
            _writer.EmitInstruction(OpCodeNumber.Ret);
            _writer.DetachInstructionSequence();
            uninitializedConstructor.SetTag(_constructorEnhancedTagGuid, "uninitialized");

            // Enhance other constructors only for entity classes.
            if (!(databaseClass is DatabaseEntityClass)) {
                return;
            }

            // Define an advice that will change the original implementation so that it calls
            // the enhanced constructor of the base class.
            advice = new EntityConstructorCallAdvice();
            _methodAdvices.Add(advice);

            ScTransformTrace.Instance.WriteLine("Inspecting constructors of the parent type.");

            foreach (IMethod referencedConstructor in parentType.Methods.GetByName(".ctor")) {
                tag = referencedConstructor.GetTag(_constructorEnhancedTagGuid);

                // We skip the constructors we have generated ourselves.
                if (tag is String) {
                    continue;
                }

                // Get the constructor that should be called instead.
                replacementConstructor = tag as IMethod;
                if (replacementConstructor == null) {
                    ScTransformTrace.Instance.WriteLine(
                                "Don't know how to map the base constructor {{{0}}}.",
                                referencedConstructor
                            );

                    referencedConstructorDef =
                        referencedConstructor.GetMethodDefinition(BindingOptions.DontThrowException);
                    if (referencedConstructorDef != null) {
                        if (referencedConstructorDef != referencedConstructor) {
                            tag = referencedConstructorDef.GetTag(_constructorEnhancedTagGuid);
                            if (tag is String) {
                                continue;
                            }
                        }

                        // Not sure about the purpose of this code, but I think it has to do
                        // with a certain handling we need for referenced constructors we have
                        // defined for built-in types. But since Entity.ctor(Uninitialized) is
                        // also equipped with HideFromApplications in Yellow, we keep the code
                        // to see when it runs if it behaves and how to possibly reimplement it.
                        // TODO:

                        //// Skip the method if it has a custom attribute "HideFromApplications".
                        //forbiddenType = (IType)referencedConstructorDef.Module.Cache.GetType(
                        //                                    typeof(HideFromApplicationsAttribute)
                        //                        );

                        //if (referencedConstructorDef.CustomAttributes.Contains(forbiddenType)) {
                        //    ScTransformTrace.Instance.WriteLine(
                        //        "Skipping this constructor because it has the [HideFromApplications] custom attribute.");
                        //    continue;
                        //}
                    }

                    // This happens when the constructor is defined outside the current module.
                    // Build the signature of the constructor we are looking for.
                    signature = new MethodSignature(_module,
                                                    referencedConstructor.CallingConvention,
                                                    referencedConstructor.ReturnType,
                                                    null,
                                                    0);

                    // Copying the original parameters to the signature.
                    for (Int32 i = 0; i < referencedConstructor.ParameterCount; i++) {
                        signature.ParameterTypes.Add(referencedConstructor.GetParameterType(i));
                    }

                    // Add the infrastructure parameters to the constructor.

                    signature.ParameterTypes.Add(_ulongType);
                    signature.ParameterTypes.Add(_typeBindingType);
                    signature.ParameterTypes.Add(_module.Cache.GetType(typeof(Uninitialized)));

                    replacementConstructor = referencedConstructor.DeclaringType.Methods.GetMethod(
                        ".ctor",
                        signature,
                        BindingOptions.Default
                        );
                    if (replacementConstructor == null) {
                        throw ErrorCode.ToException(Error.SCERRUNSPECIFIED,
                            string.Format("Cannot find the enhanced constructor of {{{0}}}.",
                                          referencedConstructor));
                    }

                    // Cache the result for next use.
                    referencedConstructor.SetTag(_constructorEnhancedTagGuid, replacementConstructor);
                }

                ScTransformTrace.Instance.WriteLine("The base constructor {{{0}}} maps to {{{1}}}.",
                                                    referencedConstructor, replacementConstructor);

                advice.AddRedirection(referencedConstructor, replacementConstructor);
            }

            // Enhance other constructors
            foreach (MethodDefDeclaration constructor in typeDef.Methods.GetByName(".ctor")) {
                if (constructor.GetTag(_constructorEnhancedTagGuid) != null) {
                    continue;
                }

                ScTransformTrace.Instance.WriteLine("Enhancing the constructor {{{0}}}.",
                                                    constructor);

                enhancedConstructor = new MethodDefDeclaration() {
                    Name = ".ctor",
                    Attributes = MethodAttributes.SpecialName
                                    | MethodAttributes.RTSpecialName
                                    | MethodAttributes.HideBySig
                                    | MethodAttributes.Public,
                    CallingConvention = CallingConvention.HasThis
                };
                typeDef.Methods.Add(enhancedConstructor);
                enhancedConstructor.ReturnParameter = new ParameterDeclaration {
                    Attributes = ParameterAttributes.Retval,
                    ParameterType = _module.Cache.GetIntrinsic(IntrinsicType.Void)
                };

                // Copy the parameters from the original constructor.
                enhancedConstructor.Parameters.AddRangeCloned(constructor.Parameters);

                // Add the infrastructure parameters to the new constructor.

                paramDecl = new ParameterDeclaration(
                    enhancedConstructor.Parameters.Count,
                    "typeAddress",
                    _ulongType
                    );
                enhancedConstructor.Parameters.Add(paramDecl);

                paramDecl = new ParameterDeclaration(
                    enhancedConstructor.Parameters.Count,
                    "typeBinding",
                    _typeBindingType
                    );
                enhancedConstructor.Parameters.Add(paramDecl);

                paramDecl = new ParameterDeclaration(
                    enhancedConstructor.Parameters.Count,
                    "dummy",
                    _uninitializedType
                    );
                enhancedConstructor.Parameters.Add(paramDecl);

                // Set some tags.
                enhancedConstructor.SetTag(_constructorEnhancedTagGuid, "enhanced");
                constructor.SetTag(_constructorEnhancedTagGuid, enhancedConstructor);

                // Move the implementation of the original constructor into the new one.
                constructorImplementation = constructor.MethodBody;
                constructor.MethodBody = new MethodBodyDeclaration();
                enhancedConstructor.MethodBody = constructorImplementation;
                constructor.CustomAttributes.Add(_weavingHelper.GetDebuggerNonUserCodeAttribute());

                // Create a new implementation of the original constructor, where we only call the new one.
                constructor.MethodBody.RootInstructionBlock
                                = constructor.MethodBody.CreateInstructionBlock();
                sequence = constructor.MethodBody.CreateInstructionSequence();
                constructor.MethodBody.RootInstructionBlock.AddInstructionSequence(sequence,
                                                                                   NodePosition.After,
                                                                                   null);
                _writer.AttachInstructionSequence(sequence);
                _writer.EmitInstruction(OpCodeNumber.Ldarg_0);

                foreach (ParameterDeclaration parameter in constructor.Parameters) {
                    _writer.EmitInstructionParameter(OpCodeNumber.Ldarg, parameter);
                }

                _writer.EmitInstructionField(OpCodeNumber.Ldsfld, typeAddressField);
                _writer.EmitInstructionField(OpCodeNumber.Ldsfld, typeBindingField);
                _writer.EmitInstruction(OpCodeNumber.Ldnull);
                _writer.EmitInstructionMethod(OpCodeNumber.Call, enhancedConstructor);
                _writer.EmitInstruction(OpCodeNumber.Ret);
                _writer.DetachInstructionSequence();

                // Sets a redirection map.
                advice.AddRedirection(constructor, enhancedConstructor);

                // Calls to the base constructor should be redirected in this constructor.
                advice.AddWovenConstructor(enhancedConstructor);
            }
        }
    }
}