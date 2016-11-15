// ***********************************************************************
// <copyright file="ScTransformTask.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using PostSharp.Extensibility;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.TypeSignatures;
using PostSharp.Sdk.CodeWeaver;
using PostSharp.Sdk.Collections;
using PostSharp.Sdk.Extensibility;
using PostSharp.Sdk.Extensibility.Tasks;
using Sc.Server.Weaver.Schema;
using Starcounter.Hosting;
using Starcounter.Internal.Weaver.BackingCode;
using Starcounter.Internal.Weaver.BackingInfrastructure;
using Starcounter.Internal.Weaver.IObjectViewImpl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

using IMember = PostSharp.Sdk.CodeModel.IMember;
using IMethod = PostSharp.Sdk.CodeModel.IMethod;

namespace Starcounter.Internal.Weaver {

    using Starcounter.Internal.Weaver.EqualityImpl;
    using DatabaseAttribute = Sc.Server.Weaver.Schema.DatabaseAttribute;

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
        /// The _cast helper
        /// </summary>
        private CastHelper _castHelper;
        /// <summary>
        /// The _DB state method provider
        /// </summary>
        private DbStateMethodProvider _dbStateMethodProvider;
        ///// <summary>
        ///// The _adapter get property method
        ///// </summary>
        //private IMethod _adapterGetPropertyMethod;
        ///// <summary>
        ///// The _adapter resolve index method
        ///// </summary>
        //private IMethod _adapterResolveIndexMethod;
        /// <summary>
        /// The _object constructor
        /// </summary>
        private IMethod _objectConstructor;
        
        private IMethod _objectGetType;
        private IMethod _typeGetFullName;

        ///// <summary>
        ///// The _obj view prop index attr constructor
        ///// </summary>
        //private IMethod _objViewPropIndexAttrConstructor;
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
        /// The _starcounter assembly reference
        /// </summary>
        private AssemblyRefDeclaration _starcounterAssemblyReference;

        private ImplementsIObjectProxy _objectProxyEmitter;
        private ImplementsEquality _equalityEmitter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScTransformTask" /> class.
        /// </summary>
        public ScTransformTask() {
            _fieldAccessors = new Dictionary<String, MethodPair>();
            _writer = new InstructionWriter();
            _fieldAdvices = new List<InsteadOfFieldAccessAdvice>();
            _methodAdvices = new List<IMethodLevelAdvice>();
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
            
            _module = this.Project.Module;
            analysisTask = ScAnalysisTask.GetTask(this.Project);

            _starcounterAssemblyReference = ScAnalysisTask.FindStarcounterAssemblyReference(_module);
            if (_starcounterAssemblyReference == null) {
                // No reference to Starcounter. We don't need to transform anything.
                // Lets skip the rest of the code.
                ScTransformTrace.Instance.WriteLine(
                    "Assembly {0} does not contain any reference to Starcounter. Skipping transformation.", _module.Name);
                return true;
            }

            ScTransformTrace.Instance.WriteLine("Transforming assembly {0}.", _module.Name);

            Initialize();

            var mainAssemblyName = Project.Properties["MainAssemblyName"];
            
            if (_module.Name.Equals(mainAssemblyName, StringComparison.InvariantCultureIgnoreCase))
            {
                var r = new ManifestResourceDeclaration()
                {
                    Name = DatabaseSchema.EmbeddedResourceName,
                    IsPublic = true
                };
                
                r.ContentStreamProvider = () =>
                {
                    var schema = ScAnalysisTask.DatabaseSchema.Serialize();
                    schema.Seek(0, System.IO.SeekOrigin.Begin);
                    return schema;
                };

                _module.AssemblyManifest.Resources.Add(r);
            }
            
            
            var assemblySpecification = new AssemblySpecificationEmit(_module);
            TypeSpecificationEmit typeSpecification = null;

            // Process database classes defined in the current assembly.

            foreach (DatabaseClass dbc in analysisTask.DatabaseClassesInCurrentModule) {
                ScTransformTrace.Instance.WriteLine("Transforming class {0}.", dbc);
                typeDef = (TypeDefDeclaration)_module.FindType(dbc.Name, BindingOptions.OnlyExisting);

                // Transformations specific to entity classes.
                databaseEntityClass = dbc as DatabaseEntityClass;
                
                typeSpecification = assemblySpecification.IncludeDatabaseClass(typeDef);
                foreach (DatabaseAttribute dba in dbc.Attributes) {
                    if (dba.IsField && dba.IsPersistent && dba.SynonymousTo == null) {
                        field = typeDef.Fields.GetByName(dba.Name);
                        typeSpecification.IncludeField(field);
                    }
                }

                ImplementIObjectProxy(typeDef);
                ImplementEquality(typeDef);
            }

            // Re-iterate all database classes in the current module and process
            // fields, constructors and synonyms.

            foreach (DatabaseClass dbc in analysisTask.DatabaseClassesInCurrentModule) {
                typeDef = (TypeDefDeclaration)_module.FindType(dbc.Name, BindingOptions.OnlyExisting);
                typeSpecification = assemblySpecification.GetSpecification(typeDef);

                // Generate field accessors and add corresponding advices
                foreach (DatabaseAttribute dba in dbc.Attributes) {
                    if (dba.IsField && dba.IsPersistent && dba.SynonymousTo == null) {
                        field = typeDef.Fields.GetByName(dba.Name);
                        var columnHandleField = typeSpecification.GetColumnHandle(dba.DeclaringClass.Name, dba.Name);
                        GenerateFieldAccessors(analysisTask, dba, field, typeSpecification, columnHandleField);
                    }
                }

                EnhanceConstructors(typeDef, dbc, typeSpecification);
                
                // Generate field accessors for synonyms
                ScTransformTrace.Instance.WriteLine("Generating synonym field accessors for {0}.", dbc);
                foreach (DatabaseAttribute dba in dbc.Attributes) {
                    if (dba.IsField && dba.IsPersistent && dba.SynonymousTo != null) {
                        ScTransformTrace.Instance.WriteLine("Generating synonym field accessor for {0}.{1}, synonmous to {2}.{3}", dbc.Name, dba.Name, dba.SynonymousTo.DeclaringClass.Name, dba.SynonymousTo.Name);
                        field = typeDef.Fields.GetByName(dba.Name);
                        var columnHandleField = typeSpecification.GetColumnHandle(dba.SynonymousTo.DeclaringClass.Name, dba.SynonymousTo.Name);
                        GenerateFieldAccessors(analysisTask, dba, field, typeSpecification, columnHandleField);
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

            // We have to index usages a second time, because we have changed method implementations.

#pragma warning disable 612
            IndexUsagesTask.Execute(this.Project);
#pragma warning restore 612
            return true;
        }

        /// <summary>
        /// Initializes and sets all fields needed for user code weavers.
        /// </summary>
        private new void Initialize() {
            IntrinsicTypeSignature voidTypeSign;
            ITypeSignature[] uninitTypeSignArr;
            
            var val = Project.Properties["UseStateRedirect"];
            bool useRedirect = !string.IsNullOrEmpty(val) && val.Equals(bool.TrueString);

            _dbStateMethodProvider = new DbStateMethodProvider(_module, null, useRedirect);
            _castHelper = new CastHelper(_module);

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

            _objectGetType = _module.FindMethod(typeof(Object).GetMethod("GetType"), BindingOptions.Default);
            _typeGetFullName = _module.FindMethod(typeof(Type).GetProperty("FullName").GetGetMethod(), BindingOptions.Default);

            _weavingHelper = new WeavingHelper(_module);

            _objectProxyEmitter = new ImplementsIObjectProxy(_module, _writer, _dbStateMethodProvider.ViewAccessMethods);
            _equalityEmitter = new ImplementsEquality(_module, _writer);
        }

        private bool ImplementIObjectProxy(TypeDefDeclaration typeDef) {
            var done = false;
            if (_objectProxyEmitter.ShouldImplementOn(typeDef)) {
                ScMessageSource.Write(SeverityType.Debug, string.Format("Implementing IObjectProxy on {0}", typeDef.Name), new Object[] { });
                _objectProxyEmitter.ImplementOn(typeDef);
                done = true;
            }
            return done;
        }

        private bool ImplementEquality(TypeDefDeclaration typeDef) {
            var done = false;
            if (_equalityEmitter.ShouldImplementOn(typeDef)) {
                ScMessageSource.Write(SeverityType.Debug, string.Format("Implementing equality on {0}", typeDef.Name), new Object[] { });
                _equalityEmitter.ImplementOn(typeDef);
                done = true;
            }
            return done;
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

            foreach (IMethodLevelAdvice advice in _methodAdvices) {
                codeWeaver.AddMethodLevelAdvice(advice,
                                                advice.TargetMethods,
                                                advice.JoinPointKinds,
                                                advice.Operands);
            }

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

        /// <summary>
        /// Generates the <b>get</b> and <b>set</b> accessors for a field, generate a property,
        /// and add an advice to replace field accesses.
        /// </summary>
        /// <param name="analysisTask">The corresponding analysis results of the current
        /// project.</param>
        /// <param name="databaseAttribute">
        /// The <see cref="DatabaseAttribute" /> for which accessors have to be generated.</param>
        /// <param name="field">The <see cref="FieldDefDeclaration" /> corresponding
        /// to <paramref name="databaseAttribute" />.</param>
        /// <param name="typeSpecification">
        /// The <see cref="TypeSpecification"/> being emitted for the type declaring the field.
        /// </param>
        /// <param name="columnHandle">The column handle field to bind to the accessors.</param>
        /// <exception cref="System.Exception"></exception>
        /// <exception cref="System.NotSupportedException"></exception>
        void GenerateFieldAccessors(
            ScAnalysisTask analysisTask,
            DatabaseAttribute databaseAttribute, 
            FieldDefDeclaration field, 
            TypeSpecificationEmit typeSpecification, 
            IField columnHandle) {
            
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
 
            ScTransformTrace.Instance.WriteLine("Generating accessors for {0}.", databaseAttribute);

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

            realDatabaseAttribute = databaseAttribute.SynonymousTo ?? databaseAttribute;
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

            var thisHandleField = typeSpecification.ThisHandle;
            var thisIdField = typeSpecification.ThisIdentity;

            getMethod.MethodBody.MaxStack = 8;
            _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
            _writer.EmitInstructionField(OpCodeNumber.Ldfld, thisIdField);
            _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
            _writer.EmitInstructionField(OpCodeNumber.Ldfld, thisHandleField);
            _writer.EmitInstructionField(OpCodeNumber.Ldsfld, columnHandle);
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
            IMethod setValueCallback;
            haveSetMethod = _dbStateMethodProvider.GetSetMethod(
                field.FieldType,
                realDatabaseAttribute,
                out dbStateMethod,
                out dbStateCast,
                out setValueCallback);
            
            if (haveSetMethod) {
                var emitSetValueCall = analysisTask.SetValueCallbacksInCurrentModule.Contains(databaseAttribute.DeclaringClass);
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

                var fullNameVariable = setMethod.MethodBody.RootInstructionBlock.DefineLocalVariable(_module.Cache.GetIntrinsic(IntrinsicType.String), "fullName");
                var keyVariable = setMethod.MethodBody.RootInstructionBlock.DefineLocalVariable(_module.Cache.GetIntrinsic(IntrinsicType.UInt64), "key");

                _writer.AttachInstructionSequence(sequence);

                _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                _writer.EmitInstructionField(OpCodeNumber.Ldfld, thisIdField);
                _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                _writer.EmitInstructionField(OpCodeNumber.Ldfld, thisHandleField);
                _writer.EmitInstructionField(OpCodeNumber.Ldsfld, columnHandle);
                _writer.EmitInstructionParameter(OpCodeNumber.Ldarg, valueParameter);

                // We have the field value on the stack, but we may need to cast it.
                if (dbStateCast != null) {
                    if (!_castHelper.EmitCast(field.FieldType, dbStateCast, _writer, ref sequence)) {
                        throw new NotSupportedException(string.Format(
                            "Don't know how to cast {0} to {1}.", field.FieldType, dbStateCast));
                    }
                }

                // TODO: Consult with Per if we can have database mapping by default.
                var emitMapCall = true; //MapConfig.Enabled;

                if (!emitSetValueCall && !emitMapCall) {
                    // We can make a tail call since it is the last instruction in the method.
                    _writer.EmitPrefix(InstructionPrefixes.Tail);
                }
                _writer.EmitInstructionMethod(OpCodeNumber.Call, dbStateMethod);

                if (emitSetValueCall) {
                    _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                    _writer.EmitInstructionField(OpCodeNumber.Ldsfld, columnHandle);
                    _writer.EmitInstructionParameter(OpCodeNumber.Ldarg, valueParameter);
                    if (dbStateCast != null) {
                        _castHelper.EmitCast(field.FieldType, dbStateCast, _writer, ref sequence);
                    }

                    _writer.EmitPrefix(InstructionPrefixes.Tail);
                    _writer.EmitInstructionMethod(OpCodeNumber.Call, setValueCallback);
                }

                if (emitMapCall) {

                    // Setup and call MapInvoke.POST
                    // This implementation is very much temporary. There are several
                    // better alternatives, where one really simple one that should
                    // perform better than this is to use TypeBinding.Name (the binding
                    // is one of the parameters) instead of object.GetType().FullName.
                    // This one works as a proof-of-concept though.

                    // Get name and store it in local variable
                    _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                    _writer.EmitInstructionMethod(OpCodeNumber.Call, _objectGetType);
                    _writer.EmitInstructionMethod(OpCodeNumber.Callvirt, _typeGetFullName);
                    _writer.EmitInstructionLocalVariable(OpCodeNumber.Stloc, fullNameVariable);

                    // Load the value of the key and store it in a local variable
                    _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                    _writer.EmitInstructionField(OpCodeNumber.Ldfld, thisIdField);
                    _writer.EmitInstructionLocalVariable(OpCodeNumber.Stloc, keyVariable);

                    // Load both variables in the stack
                    _writer.EmitInstruction(OpCodeNumber.Ldloc_0);
                    _writer.EmitInstruction(OpCodeNumber.Ldloc_1);

                    // Make the call
                    var put = _module.FindMethod(typeof(MapInvoke).GetMethod("PUT"), BindingOptions.Default);
                    _writer.EmitInstructionMethod(OpCodeNumber.Call, put);
                }

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
            
            IMethod replacementConstructor;
            InstructionSequence sequence;
            IType parentType;
            MethodBodyDeclaration constructorImplementation;
            MethodDefDeclaration enhancedConstructor;
            MethodDefDeclaration referencedConstructorDef;
            MethodSignature signature;
            Object tag;
            ParameterDeclaration paramDecl;
            EntityConstructorCallAdvice advice;
            TypeDefDeclaration parentTypeDef;
            MethodDefDeclaration insertConstructor;

            insertConstructor = null;

            // Skip if the type has already been processed.
            if (typeDef.GetTag(_constructorEnhancedTagGuid) != null) {
                return;
            }

            // Ensure that the base type has been processed.

            parentType = typeDef.BaseType;
            parentTypeDef = parentType as TypeDefDeclaration;
            if (parentTypeDef != null) {
                this.EnhanceConstructors(parentTypeDef, databaseClass.BaseClass, typeSpecification.BaseSpecification);
            }

            ScTransformTrace.Instance.WriteLine("Enhancing constructors of {0}", databaseClass);

            typeDef.SetTag(_constructorEnhancedTagGuid, "");

            // Emit the uninitialized constructor
            ScTransformTrace.Instance.WriteLine("Emitting the uninitialized constructor.");
            EmitUninitializedConstructor(typeDef);

            if (WeaverUtilities.IsDatabaseRoot(typeDef)) {
                insertConstructor = EmitInsertConstructor(typeDef, typeSpecification);
            }

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
                // These are either the uninitialized constructor, or one
                // of the generated/enhanced constructors (as created
                // below). Both store an opaque string as the tag. None
                // of them is of any interest, since we know there are
                // in fact no calls to them - they were just created.

                if (tag is String) {
                    continue;
                }

                // We found a constructor in our base type we have to replace
                // calls to, to get the upstream propagation right. Get the
                // constructor that should be called instead.

                replacementConstructor = tag as IMethod;
                if (replacementConstructor == null) {
                    ScTransformTrace.Instance.WriteLine(
                        "Don't know how to map the base constructor {{{0}}}.",
                        referencedConstructor
                        );

                    referencedConstructorDef = referencedConstructor.GetMethodDefinition(BindingOptions.DontThrowException);
                    if (referencedConstructorDef != null) {
                        if (referencedConstructorDef != referencedConstructor) {
                            tag = referencedConstructorDef.GetTag(_constructorEnhancedTagGuid);
                            if (tag is String) {
                                continue;
                            }
                        }
                    }

                    // We get here when the constructor is defined outside the current
                    // module, i.e. in a type residing in another assembly than the type
                    // we are currently weaving.

                    if (WeaverUtilities.IsDatabaseRoot(typeDef)) {
                        // For database root classes - i.e. those directly inheriting one
                        // of the allowed .NET types - we use a certain strategy: we weave
                        // against the so called "insert constructor", emitted as a private
                        // constructor in every root database class, which a unique signature.
                        // Only insert constructors in root database classes are allowed to
                        // actually create/insert objects/records in the database, and also
                        // to invoke the System.Object constructor, initializing the managed
                        // proxy. This design makes the insert constructor comparable to the
                        // .NET object constructor.
                        replacementConstructor = insertConstructor;
                    } else {
                        // The current type is a database class inheriting another database
                        // class defined in another assembly. We propagate calls to that to
                        // make sure we form a predicable, correct hierarchy.
                        signature = new MethodSignature(
                            _module,
                            referencedConstructor.CallingConvention,
                            referencedConstructor.ReturnType,
                            null,
                            0);

                        // Copy the original parameters to the signature and then add
                        // the infrastructure parameters to the constructor.

                        for (Int32 i = 0; i < referencedConstructor.ParameterCount; i++) {
                            signature.ParameterTypes.Add(referencedConstructor.GetParameterType(i));
                        }
                        signature.ParameterTypes.Add(_ushortType);
                        signature.ParameterTypes.Add(_typeBindingType);
                        signature.ParameterTypes.Add(_module.Cache.GetType(typeof(Uninitialized)));

                        // Get the constructor with the signature we just created, from the
                        // type the referenced constructor is defined in. Remember, this is
                        // still out-of-module.

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
                    }

                    // Cache the result for next use.

                    if (replacementConstructor != insertConstructor) {
                        referencedConstructor.SetTag(_constructorEnhancedTagGuid, replacementConstructor);
                    }
                }

                ScTransformTrace.Instance.WriteLine(
                    "The base constructor {{{0}}} maps to {{{1}}}.", 
                    referencedConstructor, 
                    replacementConstructor
                    );

                // Finally add the redirection to the advice.

                advice.AddRedirection(referencedConstructor, replacementConstructor);
            }

            // Enhance other constructors
            foreach (MethodDefDeclaration constructor in typeDef.Methods.GetByName(".ctor")) {
                if (constructor.GetTag(_constructorEnhancedTagGuid) != null) {
                    continue;
                }

                ScTransformTrace.Instance.WriteLine("Enhancing the constructor {{{0}}}.", constructor);

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
                // and then add the infrastructure parameters to the new constructor.

                enhancedConstructor.Parameters.AddRangeCloned(constructor.Parameters);
                paramDecl = new ParameterDeclaration(
                    enhancedConstructor.Parameters.Count,
                    "tableId",
                    _ushortType
                    );
                enhancedConstructor.Parameters.Add(paramDecl);
                var tableIdParameter = paramDecl;

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


                // Create a new implementation of the original constructor, where we
                // only call the new one. This is very simple and straight-forward.
                // We push "this", we push every parameter from the original one, we
                // push the infrastructure parameters last and make the call.
                
                constructor.CustomAttributes.Add(_weavingHelper.GetDebuggerNonUserCodeAttribute());
                constructor.MethodBody.RootInstructionBlock = constructor.MethodBody.CreateInstructionBlock();
                sequence = constructor.MethodBody.CreateInstructionSequence();
                constructor.MethodBody.RootInstructionBlock.AddInstructionSequence(
                    sequence, NodePosition.After, null);
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

                // Sets a redirection map, meaning that every call that goes
                // to the original is reimplemented to call the new, replacement
                // constructor (with added infrastructure parameters).

                advice.AddRedirection(constructor, enhancedConstructor);

                // Calls to the base constructor should be redirected in this
                // constructor.

                advice.AddWovenConstructor(enhancedConstructor);
            }
        }

        private MethodDefDeclaration EmitInsertConstructor(TypeDefDeclaration typeDef, TypeSpecificationEmit typeSpecification) {
            Trace.Assert(WeaverUtilities.IsDatabaseRoot(typeDef));

            var insertionPoint = new MethodDefDeclaration() {
                Name = ".ctor",
                Attributes = MethodAttributes.SpecialName
                                | MethodAttributes.RTSpecialName
                                | MethodAttributes.HideBySig
                                | MethodAttributes.Private,
                CallingConvention = CallingConvention.HasThis
            };
            typeDef.Methods.Add(insertionPoint);
            insertionPoint.ReturnParameter = new ParameterDeclaration {
                Attributes = ParameterAttributes.Retval,
                ParameterType = _module.Cache.GetIntrinsic(IntrinsicType.Void)
            };

            // Make sure it matches standard infrastructure parameters exactly,
            // with the exception that we'll use the Initialized type as the last
            // parameter, instead of Unitialized. This semi-hack (and making the
            // weaved replacement call pass a "casted null") allows us to reuse
            // the constructor call advice used when weaving the call hiearchy
            // between other constructors.

            var paramDecl = new ParameterDeclaration(
                insertionPoint.Parameters.Count,
                "tableId",
                _ushortType
                );
            insertionPoint.Parameters.Add(paramDecl);
            var tableIdParameter = paramDecl;
            paramDecl = new ParameterDeclaration(
                insertionPoint.Parameters.Count,
                "typeBinding",
                _typeBindingType
                );
            insertionPoint.Parameters.Add(paramDecl);
            var typeBindingParameter = paramDecl;

            paramDecl = new ParameterDeclaration(
                insertionPoint.Parameters.Count,
                "dummy",
                (IType)_module.Cache.GetType(typeof(Initialized))
                );
            insertionPoint.Parameters.Add(paramDecl);
            insertionPoint.SetTag(_constructorEnhancedTagGuid, "insert");

            insertionPoint.MethodBody = new MethodBodyDeclaration();
            insertionPoint.MethodBody.RootInstructionBlock = insertionPoint.MethodBody.CreateInstructionBlock();

            var fullNameVariable = insertionPoint.MethodBody.RootInstructionBlock.DefineLocalVariable(_module.Cache.GetIntrinsic(IntrinsicType.String), "fullName");
            var keyVariable = insertionPoint.MethodBody.RootInstructionBlock.DefineLocalVariable(_module.Cache.GetIntrinsic(IntrinsicType.UInt64), "key");

            var sequence = insertionPoint.MethodBody.CreateInstructionSequence();
            insertionPoint.MethodBody.RootInstructionBlock.AddInstructionSequence(
                sequence,
                NodePosition.After,
                null);
            _writer.AttachInstructionSequence(sequence);

            _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
            _writer.EmitInstructionMethod(OpCodeNumber.Call, _objectConstructor);
            _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
            _writer.EmitInstructionParameter(OpCodeNumber.Ldarg, typeBindingParameter);
            _writer.EmitInstructionField(OpCodeNumber.Stfld, typeSpecification.ThisBinding);
            _writer.EmitInstructionParameter(OpCodeNumber.Ldarg, tableIdParameter);
            _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
            _writer.EmitInstructionField(OpCodeNumber.Ldflda, typeSpecification.ThisIdentity);
            _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
            _writer.EmitInstructionField(OpCodeNumber.Ldflda, typeSpecification.ThisHandle);
            _writer.EmitInstructionMethod(OpCodeNumber.Call,
                _module.FindMethod(_dbStateMethodProvider.DbStateType.GetMethod("Insert"), BindingOptions.Default));

            // TODO: Consult with Per if we can have database mapping by default.
            var emitMapCall = true; //MapConfig.Enabled;

            if (emitMapCall) {
                // Setup and call MapInvoke.POST
                // This implementation is very much temporary. There are several
                // better alternatives, where one really simple one that should
                // perform better than this is to use TypeBinding.Name (the binding
                // is one of the parameters) instead of object.GetType().FullName.
                // This one works as a proof-of-concept though.

                _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                _writer.EmitInstructionMethod(OpCodeNumber.Call, _objectGetType);
                _writer.EmitInstructionMethod(OpCodeNumber.Callvirt, _typeGetFullName);
                _writer.EmitInstructionLocalVariable(OpCodeNumber.Stloc, fullNameVariable);
                _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                var idField = typeDef.Fields.GetByName(TypeSpecification.ThisIdName);
                _writer.EmitInstructionField(OpCodeNumber.Ldfld, idField);
                _writer.EmitInstructionLocalVariable(OpCodeNumber.Stloc, keyVariable);
                _writer.EmitInstruction(OpCodeNumber.Ldloc_0);
                _writer.EmitInstruction(OpCodeNumber.Ldloc_1);

                var post = _module.FindMethod(typeof(MapInvoke).GetMethod("POST"), BindingOptions.Default);
                _writer.EmitInstructionMethod(OpCodeNumber.Call, post);
            }

            _writer.EmitInstruction(OpCodeNumber.Ret);
            _writer.DetachInstructionSequence();
            
            return insertionPoint;
        }

        private MethodDefDeclaration EmitUninitializedConstructor(TypeDefDeclaration typeDef) {
            CustomAttributeDeclaration customAttr;
            ParameterDeclaration paramDecl;

            var uninitializedConstructor = new MethodDefDeclaration {
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

            var rootInstrBlock = uninitializedConstructor.MethodBody.CreateInstructionBlock();
            uninitializedConstructor.MethodBody.RootInstructionBlock = rootInstrBlock;

            var sequence = uninitializedConstructor.MethodBody.CreateInstructionSequence();
            rootInstrBlock.AddInstructionSequence(sequence, NodePosition.After, null);
            _writer.AttachInstructionSequence(sequence);
            if (WeaverUtilities.IsDatabaseRoot(typeDef)) {
                _writer.EmitInstruction(OpCodeNumber.Ldarg_0);
                _writer.EmitInstructionMethod(OpCodeNumber.Call, _objectConstructor);
            } else {
                var baseUninitializedConstructor = typeDef.BaseType.Methods.GetMethod(".ctor",
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

            return uninitializedConstructor;
        }
    }
}