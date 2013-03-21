// ***********************************************************************
// <copyright file="ScTransformTask.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

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
using Sc.Server.Weaver.Schema;
using Starcounter;
using Starcounter.LucentObjects;
using IMember = PostSharp.Sdk.CodeModel.IMember;
using IMethod = PostSharp.Sdk.CodeModel.IMethod;
using Starcounter.Internal.Weaver.IObjectViewImpl;
using Starcounter.Internal.Weaver.BackingInfrastructure;
using Starcounter.Hosting;
using DatabaseAttribute = Sc.Server.Weaver.Schema.DatabaseAttribute;
using Starcounter.Internal.Weaver.BackingCode;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// PostSharp task responsible for transforming the assembly. It also implements the
    /// <see cref="IAdviceProvider" /> interface to provide advices to the low-level
    /// code weaver.
    /// </summary>
#pragma warning disable 618
    public class ScTransformTask : Task, IAdviceProvider {
#pragma warning restore 618

        /// <summary>
        /// The _constructor enhanced tag GUID
        /// </summary>
        private static readonly TagId _constructorEnhancedTagGuid
                                        = TagId.Register("{A7296EEE-BD8D-4220-9153-B5AAE974FA98}");

        /// <summary>
        /// The _field accessors
        /// </summary>
        private readonly Dictionary<String, MethodPair> _fieldAccessors;
        /// <summary>
        /// The _writer
        /// </summary>
        private readonly InstructionWriter _writer;
        /// <summary>
        /// The _field advices
        /// </summary>
        private readonly List<InsteadOfFieldAccessAdvice> _fieldAdvices;
        /// <summary>
        /// The _method advices
        /// </summary>
        private readonly List<IMethodLevelAdvice> _methodAdvices;
        /// <summary>
        /// The _weaved lucent accessor advices
        /// </summary>
        private readonly List<ReimplementWeavedLucentAccessorAdvice> _weavedLucentAccessorAdvices;

        /// <summary>
        /// The _cast helper
        /// </summary>
        private CastHelper _castHelper;
        /// <summary>
        /// The _DB state method provider
        /// </summary>
        private DbStateMethodProvider _dbStateMethodProvider;
        /// <summary>
        /// The _adapter get property method
        /// </summary>
        private IMethod _adapterGetPropertyMethod;
        /// <summary>
        /// The _adapter resolve index method
        /// </summary>
        private IMethod _adapterResolveIndexMethod;
        /// <summary>
        /// The _object constructor
        /// </summary>
        private IMethod _objectConstructor;
        /// <summary>
        /// The _obj view prop index attr constructor
        /// </summary>
        private IMethod _objViewPropIndexAttrConstructor;
        /// <summary>
        /// The _uninitialized constructor signature
        /// </summary>
        private IMethodSignature _uninitializedConstructorSignature;
        /// <summary>
        /// The _nullable type
        /// </summary>
        private INamedType _nullableType;
        /// <summary>
        /// The _uninitialized type
        /// </summary>
        private IType _uninitializedType;
        /// <summary>
        /// The _type binding type
        /// </summary>
        private ITypeSignature _typeBindingType;
        /// <summary>
        /// </summary>
        private ITypeSignature _ushortType;
        /// <summary>
        /// The _object view type
        /// </summary>
        private ITypeSignature _objectViewType;
        /// <summary>
        /// The _module
        /// </summary>
        private ModuleDeclaration _module;
        /// <summary>
        /// The _weaving helper
        /// </summary>
        private WeavingHelper _weavingHelper;
        /// <summary>
        /// The _weave for IPC
        /// </summary>
        private bool _weaveForIPC;
        /// <summary>
        /// The _starcounter assembly reference
        /// </summary>
        private AssemblyRefDeclaration _starcounterAssemblyReference;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScTransformTask" /> class.
        /// </summary>
        public ScTransformTask() {
            _fieldAccessors = new Dictionary<String, MethodPair>();
            _writer = new InstructionWriter();
            _fieldAdvices = new List<InsteadOfFieldAccessAdvice>();
            _methodAdvices = new List<IMethodLevelAdvice>();
            _weavedLucentAccessorAdvices = new List<ReimplementWeavedLucentAccessorAdvice>();
        }

        /// <summary>
        /// Gets the field name, but without the assembly name.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <returns>System.String.</returns>
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
        private void GetAccessors(IField field, out IMethod getMethod, out IMethod setMethod) {
            MethodPair pair;
            MethodSignature signature;

            var fieldName = GetFieldName(field);
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
            /// <summary>
            /// The get method
            /// </summary>
            public IMethod GetMethod;
            /// <summary>
            /// The set method
            /// </summary>
            public IMethod SetMethod;
        }

        /// <summary>
        /// Entry point of the current task.
        /// </summary>
        /// <returns><b>true</b> in case of success, otherwise <b>false</b>.</returns>
        public override bool Execute() {
            DatabaseAttribute databaseAttribute;
            DatabaseClass databaseClass;
            DatabaseEntityClass databaseEntityClass;
            FieldDefDeclaration field;
            IEnumerator<MetadataDeclaration> typeEnumerator;
            IMethod getMethod;
            IMethod setMethod;
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

                ScMessageSource.Write(SeverityType.Info, "SCINF03", new Object[] { _module.Name });
                return true;
            }

            // Check if the transformation kind has been established to be None,
            // meaning we need not to transform at all.

            if (analysisTask.TransformationKind == WeaverTransformationKind.None) {
                ScMessageSource.Write(
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

            ScMessageSource.Write(SeverityType.ImportantInfo, "SCINF02", new Object[] { _module.Name });

            // Initialize extra for user code weavers.

            InitializeForUserCodeWeavers();

            if (_weaveForIPC) {
                weavedAssemblyAttributeCtor = _module.FindMethod(
                    typeof(AssemblyWeavedForIPCAttribute).GetConstructor(Type.EmptyTypes),
                    BindingOptions.Default
                    );
                _module.AssemblyManifest.CustomAttributes.Add(new CustomAttributeDeclaration(weavedAssemblyAttributeCtor));
            }

            var assemblySpecification = new AssemblySpecificationEmit(_module);
            var typeSpecification = new TypeSpecificationEmit(_module);

            // Process database classes defined in the current assembly.

            foreach (DatabaseClass dbc in analysisTask.DatabaseClassesInCurrentModule) {
                ScTransformTrace.Instance.WriteLine("Transforming {0}.", dbc);
                typeDef = (TypeDefDeclaration)_module.FindType(dbc.Name, BindingOptions.OnlyExisting);

                // Transformations specific to entity classes.
                databaseEntityClass = dbc as DatabaseEntityClass;
                if (databaseEntityClass != null) {
                    assemblySpecification.IncludeDatabaseClass(typeDef);
                    typeSpecification.EmitForType(typeDef);
                    if (InheritsObject(typeDef)) {
                        ImplementIObjectProxy(typeDef);
                    }
                }

                // Generate field accessors and add corresponding advices
                foreach (DatabaseAttribute dba in dbc.Attributes) {
                    if (dba.IsField && dba.IsPersistent && dba.SynonymousTo == null) {
                        field = typeDef.Fields.GetByName(dba.Name);
                        var columnHandleField = typeSpecification.IncludeField(typeDef, field);
                        GenerateFieldAccessors(dba, field, columnHandleField);
                    }
                }
            }

            // Re-iterate all database classes in the current module and process
            // constructors and synonyms.

            foreach (DatabaseClass dbc in analysisTask.DatabaseClassesInCurrentModule) {
                typeDef = (TypeDefDeclaration)_module.FindType(dbc.Name, BindingOptions.OnlyExisting);
                EnhanceConstructors(typeDef, dbc, typeSpecification);

                // Generate field accessors for synonyms
                foreach (DatabaseAttribute dba in dbc.Attributes) {
                    if (dba.IsField && dba.IsPersistent && dba.SynonymousTo != null) {
                        field = typeDef.Fields.GetByName(dba.Name);
                        GenerateFieldAccessors(dba, field);
                    }
                }
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

#pragma warning disable 612
            IndexUsagesTask.Execute(this.Project);
#pragma warning restore 612
            return true;
        }

        /// <summary>
        /// Executes the on IPC weaved assembly.
        /// </summary>
        /// <param name="analysisTask">The analysis task.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
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

        /// <summary>
        /// Initializes for all transformation kinds.
        /// </summary>
        private void InitializeForAllTransformationKinds() {
            string dynamicLibDir;

            // Only consider using the dynamic / code generated library if we are
            // weaving inside the database, not when weaving targetting an IPC-ish
            // context.

            dynamicLibDir = _weaveForIPC
                ? null
                : Project.Properties["ScDynamicLibInputDir"];

            var val = Project.Properties["UseStateRedirect"];
            bool useRedirect = !string.IsNullOrEmpty(val) && val.Equals(bool.TrueString);

            _dbStateMethodProvider = new DbStateMethodProvider(_module, dynamicLibDir, useRedirect);
            _castHelper = new CastHelper(_module);
        }

        /// <summary>
        /// Initializes for user code weavers.
        /// </summary>
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
            _uninitializedConstructorSignature = new MethodSignature(
                _module,
                CallingConvention.HasThis,
                voidTypeSign,
                uninitTypeSignArr,
                0);
            _nullableType = (INamedType)_module.FindType(typeof(Nullable<>), BindingOptions.RequireGenericDefinition);

            _typeBindingType = _module.Cache.GetType(typeof(Starcounter.Binding.TypeBinding));
            _ushortType = _module.Cache.GetIntrinsic(IntrinsicType.UInt16);

            _objectViewType = _module.FindType(typeof(IObjectView), BindingOptions.Default);

            _objectConstructor = _module.FindMethod(
                typeof(Object).GetConstructor(Type.EmptyTypes), BindingOptions.Default);

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
        }

        /// <summary>
        /// Searching for the Starcounter assembly reference from the current modules references.
        /// </summary>
        /// <returns>The assembly reference, or null if not found.</returns>
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
        /// Finds the type of the starcounter.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>IType.</returns>
        /// <exception cref="System.InvalidOperationException"></exception>
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
        /// Enhances the type of the anonymous.
        /// </summary>
        /// <param name="typeDef">The type def.</param>
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
#pragma warning disable 618
            objectViewFieldRef = GenericHelper.GetFieldCanonicalGenericInstance(objectViewField);
#pragma warning restore 618

            adapterField = new FieldDefDeclaration() {
                Name = "adapter",
                Attributes =
                FieldAttributes.Private | FieldAttributes.InitOnly,
                FieldType =
                this._module.Cache.GetType(typeof(AnonymousTypeAdapter))
            };
            typeDef.Fields.Add(adapterField);
#pragma warning disable 618
            adapterFieldRef = GenericHelper.GetFieldCanonicalGenericInstance(adapterField);
#pragma warning restore 618

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
#pragma warning disable 618
                        _weavingHelper.FromObject(targetValueType, _writer);
#pragma warning restore 618
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
        /// Determines whether [is anonymous type] [the specified type def].
        /// </summary>
        /// <param name="typeDef">The type def.</param>
        private static Boolean IsAnonymousType(TypeDefDeclaration typeDef) {
            return typeDef.Name.StartsWith("<>f__AnonymousType")
                    || typeDef.Name.StartsWith("VB$AnonymousType");
        }

        /// <summary>
        /// Gets a value indicating if the given type directly inherits the
        /// root .NET type System.Object.
        /// </summary>
        /// <param name="typeDef">The type to check</param>
        /// <returns>True if it inherits object direct; false otherwise.</returns>
        private static bool InheritsObject(TypeDefDeclaration typeDef) {
            return ScAnalysisTask.Inherits(typeDef, typeof(object).FullName, true);
        }

        private void ImplementIObjectProxy(TypeDefDeclaration typeDef) {
            ScMessageSource.Write(SeverityType.Info, string.Format("Implementing IObjectProxy/IObjectView for {0}", typeDef.Name), new Object[] {});
            new ImplementsIObjectProxy(_module, _writer, _dbStateMethodProvider.ViewAccessMethods).ImplementOn(typeDef);
        }

        /// <summary>
        /// Method called by the code weaver when it wants to know which advices
        /// we want to provide.
        /// </summary>
        /// <param name="codeWeaver">The code weaver to which we should provide advices.</param>
        public void ProvideAdvices(PostSharp.Sdk.CodeWeaver.Weaver codeWeaver) {
            Singleton<MetadataDeclaration> metaSingleton;
            
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

            foreach (IMethodLevelAdvice advice in _methodAdvices) {
                codeWeaver.AddMethodLevelAdvice(advice,
                                                advice.TargetMethods,
                                                advice.JoinPointKinds,
                                                advice.Operands);
            }

            // If a initializer has been defined, make sure we attach an advice that will
            // implement it.

            if (_weaveForIPC) {
                DatabaseClassInitCallMethodAdvice pcia = new DatabaseClassInitCallMethodAdvice();

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
        /// <param name="databaseAttribute">The <see cref="DatabaseAttribute" /> for which accessors have to be generated.</param>
        /// <param name="field">The <see cref="FieldDefDeclaration" /> corresponding
        /// to <paramref name="databaseAttribute" />.</param>
        /// <param name="columnHandle">The column handle field to bind to the accessors.</param>
        /// <exception cref="System.Exception"></exception>
        /// <exception cref="System.NotSupportedException"></exception>
        void GenerateFieldAccessors(
            DatabaseAttribute databaseAttribute, 
            FieldDefDeclaration field, 
            IField columnHandle = null) {
            
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
            IField attributeIndexField;
            DatabaseAttribute synonymousTo;

            // Since we currently support just a single weaving mechanism,
            // I'll try cleaning this method up a bit. One of the things is
            // to remove alternative implementation for different weaving
            // targets.
            Trace.Assert(_weaveForIPC);

            ScTransformTrace.Instance.WriteLine("Generating accessors for {0}.", databaseAttribute);

            attributeIndexField = null;
            synonymousTo = databaseAttribute.SynonymousTo;
            realDatabaseAttribute = synonymousTo ?? databaseAttribute;

            #region Soon obsolete (lookup of attribute index field)
            
            if (synonymousTo == null) {
                // Generate a static field that can hold the attribute index in
                // hosted environments.

                // To be removed.
                // TODO:

                //attributeIndexField = new FieldDefDeclaration {
                //    Name = WeaverNamingConventions.MakeAttributeIndexVariableName(field.Name),
                //    Attributes = (FieldAttributes.Family | FieldAttributes.Static),
                //    FieldType = _module.Cache.GetIntrinsic(IntrinsicType.Int32)
                //};

                //field.DeclaringType.Fields.Add((FieldDefDeclaration)attributeIndexField);
                //_weavingHelper.AddCompilerGeneratedAttribute(attributeIndexField.CustomAttributes);

            } else {

                // To be rewritten.
                // TODO:

                var nameOfAttributeIndexField = WeaverNamingConventions.MakeAttributeIndexVariableName(synonymousTo.Name);

                // We must locate the type of the target field. It might be a definition, or a
                // type reference.

                var synonymTargetType = (IType)_module.FindType(synonymousTo.DeclaringClass.Name, BindingOptions.OnlyExisting | BindingOptions.DontThrowException);
                if (synonymTargetType == null) {
                    var typeEnumerator = _module.GetDeclarationEnumerator(TokenType.TypeRef);
                    while (typeEnumerator.MoveNext()) {
                        var typeRef = (TypeRefDeclaration)typeEnumerator.Current;
                        if (ScAnalysisTask.GetTypeReflectionName(typeRef).Equals(synonymousTo.DeclaringClass.Name)) {
                            synonymTargetType = typeRef;
                            break;
                        }
                    }
                }

                // Lets not do any verification here, since the analyzer should already
                // have done that for us. We simply rely on both the type and the field
                // to be found, or else let a null reference exception be raised.

                attributeIndexField = synonymTargetType.Fields.GetField(
                    nameOfAttributeIndexField, synonymTargetType.Module.FindType(typeof(int)), BindingOptions.Default);
            }
            #endregion

            if (columnHandle != null)
                attributeIndexField = columnHandle;

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

            // First generate a get method.

            _dbStateMethodProvider.GetGetMethod(field.FieldType, realDatabaseAttribute, out dbStateMethod, out dbStateCast);

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
            getMethod.MethodBody.RootInstructionBlock.AddInstructionSequence(sequence, NodePosition.After, null);
            _writer.AttachInstructionSequence(sequence);

            // We need the this handle and the this identity fields from
            // the type defining the field we are about to replace, to make
            // the proper call to the correct DbState method.
            //   These, we should obviosly cache on the type level for all
            // future field generation rounds.
            // TODO:

            var thisHandleField = field.DeclaringType.Fields.GetByName(TypeSpecification.ThisHandleName);
            var thisIdField = field.DeclaringType.Fields.GetByName(TypeSpecification.ThisIdName);

            if (dbStateMethod.ParameterCount == 3) {
                getMethod.MethodBody.MaxStack = 8;
                _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                _writer.EmitInstructionField(OpCodeNumber.Ldfld, thisIdField);
                _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                _writer.EmitInstructionField(OpCodeNumber.Ldfld, thisHandleField);
                _writer.EmitInstructionField(OpCodeNumber.Ldsfld, attributeIndexField);
                if (dbStateCast == null) {
                    // If we don't have to cast the value, we can use a 'tail' call.
                    _writer.EmitPrefix(InstructionPrefixes.Tail);
                }
                _writer.EmitInstructionMethod(OpCodeNumber.Call, dbStateMethod);

            } else {

                // We will call a method with this stack transition:
                // <this>, <index> -> <value>
                _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                _writer.EmitInstructionField(OpCodeNumber.Ldsfld, attributeIndexField);
                if (dbStateCast == null) {
                    // If we don't have to cast the value, we can use a 'tail' call.
                    _writer.EmitPrefix(InstructionPrefixes.Tail);
                }
                _writer.EmitInstructionMethod(OpCodeNumber.Call, dbStateMethod);
            }

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
            haveSetMethod = _dbStateMethodProvider.GetSetMethod(
                field.FieldType,
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
                setMethod.MethodBody.RootInstructionBlock = setMethod.MethodBody.CreateInstructionBlock();
                sequence = setMethod.MethodBody.CreateInstructionSequence();
                setMethod.MethodBody.RootInstructionBlock.AddInstructionSequence(sequence, NodePosition.After, null);
                _writer.AttachInstructionSequence(sequence);

                if (dbStateMethod.ParameterCount == 4) {
                    //.maxstack 8
                    //L_0000: ldarg.0 
                    //L_0001: ldfld uint64 TestAccess::thisId
                    //L_0006: ldarg.0 
                    //L_0007: ldfld uint64 TestAccess::thisHandle
                    //L_000c: ldsfld int32 TestAccess/Spec::Foo1
                    //L_0011: ldarg.1 
                    //L_0012: call void MyDbState::WriteString(uint64, uint64, int32, string)
                    //L_0017: ret
                    _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                    _writer.EmitInstructionField(OpCodeNumber.Ldfld, thisIdField);
                    _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                    _writer.EmitInstructionField(OpCodeNumber.Ldfld, thisHandleField);
                    _writer.EmitInstructionField(OpCodeNumber.Ldsfld, attributeIndexField);
                    _writer.EmitInstructionParameter(OpCodeNumber.Ldarg, valueParameter);

                } else {

                    // We need to prepare this stack transition:
                    // <this>, <index>, <casted_value> -> void

                    _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                    _writer.EmitInstructionField(OpCodeNumber.Ldsfld, attributeIndexField);
                    _writer.EmitInstructionParameter(OpCodeNumber.Ldarg, valueParameter);
                }

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

            RemoveTask.GetTask(Project).MarkForRemoval(field);
            
            _fieldAdvices.Add(new InsteadOfFieldAccessAdvice(field, getMethod, setMethod));
        }

        /// <summary>
        /// Enhances the constructors.
        /// </summary>
        /// <param name="typeDef">The type def.</param>
        /// <param name="databaseClass">The database class.</param>
        private void EnhanceConstructors(
            TypeDefDeclaration typeDef, 
            DatabaseClass databaseClass,
            TypeSpecificationEmit typeSpecification) {
            
            IMethod baseUninitializedConstructor;
            IMethod replacementConstructor;
            InstructionBlock rootInstrBlock;
            InstructionSequence sequence;
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

            // Skip if the type has already been processed.
            if (typeDef.GetTag(_constructorEnhancedTagGuid) != null) {
                return;
            }

            // Ensure that the base type has been processed.
            parentType = typeDef.BaseType;
            parentTypeDef = parentType as TypeDefDeclaration;

            if (parentTypeDef != null) {
                this.EnhanceConstructors(parentTypeDef, databaseClass.BaseClass, typeSpecification);    // TODO: Base type specification!
            }

            ScTransformTrace.Instance.WriteLine("Enhancing constructors of {0}", databaseClass);

            typeDef.SetTag(_constructorEnhancedTagGuid, "");

            // Emit the uninitialized constructor
            ScTransformTrace.Instance.WriteLine("Emitting the uninitialized constructor.");

            CreateAndAddUninitializedConstructorSignature(typeDef, out uninitializedConstructor);

            rootInstrBlock = uninitializedConstructor.MethodBody.CreateInstructionBlock();
            uninitializedConstructor.MethodBody.RootInstructionBlock = rootInstrBlock;

            sequence = uninitializedConstructor.MethodBody.CreateInstructionSequence();
            rootInstrBlock.AddInstructionSequence(sequence, NodePosition.After, null);
            _writer.AttachInstructionSequence(sequence);
            if (!InheritsObject(typeDef)) {
                baseUninitializedConstructor = parentType.Methods.GetMethod(".ctor",
                    _uninitializedConstructorSignature,
                    BindingOptions.Default
                    );
                _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                _writer.EmitInstruction(OpCodeNumber.Ldnull);
                _writer.EmitInstructionMethod(OpCodeNumber.Call, baseUninitializedConstructor);
            }
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

            if (!InheritsObject(typeDef)) {
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

                        signature.ParameterTypes.Add(_ushortType);
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
                    "tableId",
                    _ushortType
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

                if (InheritsObject(typeDef)) {
                    // This will be a real challenge - we need to inject the call
                    // to DbState.Insert just after the object is actually created.
                    // The constructor being "moved" here generally will contain
                    // that code, so we can't just use the "first line principle".
                    //   For now, we'll just assume an empty ctor calling object:ctor
                    // and we'll do our stuff after that (or, actually, we are just
                    // replacing the whole thingy.
                    //   When implementing the final solution, consider if it's a
                    // good approach to have every root have a private ctor with a
                    // hidden signature that is responsible for the call to insert.
                    // TODO:
                    enhancedConstructor.MethodBody = new MethodBodyDeclaration();
                    enhancedConstructor.MethodBody.RootInstructionBlock
                                = enhancedConstructor.MethodBody.CreateInstructionBlock();
                    sequence = enhancedConstructor.MethodBody.CreateInstructionSequence();
                    enhancedConstructor.MethodBody.RootInstructionBlock.AddInstructionSequence(
                        sequence,
                        NodePosition.After,
                        null);
                    _writer.AttachInstructionSequence(sequence);

                    _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                    _writer.EmitInstructionMethod(OpCodeNumber.Call, _objectConstructor);
                    _writer.EmitInstruction(OpCodeNumber.Ldarg_1);
                    _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                    _writer.EmitInstructionField(OpCodeNumber.Ldflda, typeSpecification.ThisIdentity);
                    _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                    _writer.EmitInstructionField(OpCodeNumber.Ldflda, typeSpecification.ThisHandle);
                    _writer.EmitInstructionMethod(OpCodeNumber.Call,
                        _module.FindMethod(_dbStateMethodProvider.DbStateType.GetMethod("Insert"), BindingOptions.Default));
                    _writer.EmitInstruction(OpCodeNumber.Ret);
                    _writer.DetachInstructionSequence();
                }

                // Create a new implementation of the original constructor, where we
                // only call the new one.
                constructor.CustomAttributes.Add(_weavingHelper.GetDebuggerNonUserCodeAttribute());
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

                _writer.EmitInstructionField(OpCodeNumber.Ldsfld, typeSpecification.TableHandle);
                _writer.EmitInstructionField(OpCodeNumber.Ldsfld, typeSpecification.TypeBindingReference);
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

        private void CreateAndAddUninitializedConstructorSignature(
            TypeDefDeclaration typeDef, out MethodDefDeclaration uninitializedConstructor) {
            CustomAttributeDeclaration customAttr;
            ParameterDeclaration paramDecl;

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
        }
    }
}