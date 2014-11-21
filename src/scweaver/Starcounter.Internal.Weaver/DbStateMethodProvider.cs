// ***********************************************************************
// <copyright file="DbStateMethodProvider.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.TypeSignatures;
using Sc.Server.Weaver.Schema;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using IMethod = PostSharp.Sdk.CodeModel.IMethod;

namespace Starcounter.Internal.Weaver {

    using DatabaseAttribute = Sc.Server.Weaver.Schema.DatabaseAttribute;

    /// <summary>
    /// Provides the method <see cref="GetGetMethod" /> and <see cref="GetSetMethod" />, which determine
    /// which method of the <see cref="DbState" /> class should be called to retrieve or store the value
    /// of database fields, and which type conversion is necessary.
    /// </summary>
    internal class DbStateMethodProvider {
        /// <summary>
        /// Class MethodCastPair
        /// </summary>
        private class MethodCastPair {
            /// <summary>
            /// The primary access method (e.g. DbState.ReadString, DbState.WriteObject)
            /// </summary>
            public IMethod Method;

            /// <summary>
            /// The cast type, if a cast is needed before invoking the actual method.
            /// </summary>
            public ITypeSignature CastType;

            /// <summary>
            /// The method to invoke if the <see cref="ISetValueCallback"/> interface
            /// is defined on the database class we provide methods for, and this
            /// pair defines a write operation. Null if nothing of that applies.
            /// </summary>
            public IMethod SetValueCallbackMethod;
        }

        internal sealed class ViewAccessors {
            
            public ViewAccessors(ModuleDeclaration module) {
                BindingOptions bind = BindingOptions.Default;
                var viewType = typeof(DbState.View);
                GetBoolean = module.FindMethod(viewType.GetMethod("GetBoolean"), bind);
                GetBinary = module.FindMethod(viewType.GetMethod("GetBinary"), bind);
                GetByte = module.FindMethod(viewType.GetMethod("GetByte"), bind);
                GetDateTime = module.FindMethod(viewType.GetMethod("GetDateTime"), bind);
                GetDecimal = module.FindMethod(viewType.GetMethod("GetDecimal"), bind);
                GetDouble = module.FindMethod(viewType.GetMethod("GetDouble"), bind);
                GetInt16 = module.FindMethod(viewType.GetMethod("GetInt16"), bind);
                GetInt32 = module.FindMethod(viewType.GetMethod("GetInt32"), bind);
                GetInt64 = module.FindMethod(viewType.GetMethod("GetInt64"), bind);
                GetObject = module.FindMethod(viewType.GetMethod("GetObject"), bind);
                GetSByte = module.FindMethod(viewType.GetMethod("GetSByte"), bind);
                GetSingle = module.FindMethod(viewType.GetMethod("GetSingle"), bind);
                GetString = module.FindMethod(viewType.GetMethod("GetString"), bind);
                GetUInt16 = module.FindMethod(viewType.GetMethod("GetUInt16"), bind);
                GetUInt32 = module.FindMethod(viewType.GetMethod("GetUInt32"), bind);
                GetUInt64 = module.FindMethod(viewType.GetMethod("GetUInt64"), bind);
            }

            public IMethod GetBoolean { get; private set; }
            public IMethod GetBinary { get; private set; }
            public IMethod GetByte{ get; private set; }
            public IMethod GetDateTime { get; private set; }
            public IMethod GetDecimal { get; private set; }
            public IMethod GetDouble { get; private set; }
            public IMethod GetInt16 { get; private set; }
            public IMethod GetInt32 { get; private set; }
            public IMethod GetInt64 { get; private set; }
            public IMethod GetObject { get; private set; }
            public IMethod GetSByte { get; private set; }
            public IMethod GetSingle { get; private set; }
            public IMethod GetString { get; private set; }
            public IMethod GetUInt16 { get; private set; }
            public IMethod GetUInt32 { get; private set; }
            public IMethod GetUInt64 { get; private set; }
        }

        /// <summary>
        /// The read operation
        /// </summary>
        private const string readOperation = "Read";
        /// <summary>
        /// The write operation
        /// </summary>
        private const string writeOperation = "Write";

        /// <summary>
        /// The module
        /// </summary>
        private readonly ModuleDeclaration module;
        /// <summary>
        /// The cache
        /// </summary>
        private readonly Dictionary<string, MethodCastPair> cache = new Dictionary<string, MethodCastPair>();
        /// <summary>
        /// The db state type
        /// </summary>
        private readonly Type dbStateType;
        /// <summary>
        /// The code generated db state type
        /// </summary>
        private readonly Type codeGeneratedDbStateType;
        /// <summary>
        /// Gets the set of (cached) view access methods we bind to when
        /// implementing the IObjectView data-retreival methods.
        /// </summary>
        public ViewAccessors ViewAccessMethods { get; private set; }

        public Type DbStateType { get { return dbStateType; } }

        public Type SetValueCallbackFacadeType { get; private set; }

        /// <summary>
        /// Initializes a new <see cref="DbStateMethodProvider" />.
        /// </summary>
        /// <param name="module">Modules from which the <see cref="DbState" /> methods will be called.</param>
        /// <param name="dynamicLibDir">The dynamic lib dir.</param>
        public DbStateMethodProvider(ModuleDeclaration module, string dynamicLibDir, bool useStateRedirect = false) {
            Trace.Assert(
                string.IsNullOrEmpty(dynamicLibDir), "Currently, we don't support generated code.");

            this.module = module;
            this.codeGeneratedDbStateType = null;
            this.ViewAccessMethods = new ViewAccessors(module);
            this.dbStateType = useStateRedirect ? typeof(Starcounter.Hosting.DbStateRedirect) : typeof(DbState);
            SetValueCallbackFacadeType = typeof(SetValueCallbackInvoke);
        }

        #region Helper methods
        /// <summary>
        /// Gets the cache key of the type of a <see cref="DatabaseAttribute" />.
        /// </summary>
        /// <param name="databaseAttribute">A <see cref="DatabaseAttribute" />.</param>
        /// <returns>A string uniquely identifying the type of the <paramref name="databaseAttribute" />.</returns>
        private static string GetAttributeTypeCacheKey(DatabaseAttribute databaseAttribute) {
            return databaseAttribute.AttributeType.ToString() + ", nullable=" + databaseAttribute.IsNullable.ToString();
        }

        /// <summary>
        /// Gets a generic method instance of <see cref="DbState" />. The method is supposed to have
        /// a single generic parameter.
        /// </summary>
        /// <param name="methodName">Name of the method instance.</param>
        /// <param name="itemType">Generic argument.</param>
        /// <returns>A generic instance of the method of <see cref="DbState" /> named
        /// <paramref name="methodName" /> with <paramref name="itemType" /> as the only generic
        /// argument.</returns>
        private IMethod GetGenericMethodInstance(string methodName, ITypeSignature itemType) {
            MethodInfo methodInfo = dbStateType.GetMethod(methodName);
            Trace.Assert(methodInfo != null, string.Format("Cannot find the method DbState.{0}.", methodName));
            MethodRefDeclaration methodRef = (MethodRefDeclaration)
                                             this.module.FindMethod(methodInfo, BindingOptions.RequireGenericDefinition);
            return methodRef.MethodSpecs.GetGenericInstance(new ITypeSignature[] { itemType }, true);
        }

        /// <summary>
        /// Maps a <see cref="DatabasePrimitive" /> on an <see cref="IntrinsicType" />.
        /// </summary>
        /// <param name="primitive">A <see cref="DatabasePrimitive" />.</param>
        /// <returns>The <see cref="IntrinsicType" /> corresponding to <paramref name="primitive" />.</returns>
        private static IntrinsicType MapDatabasePrimitiveToInstrinsic(DatabasePrimitive primitive) {
            switch (primitive) {
                case DatabasePrimitive.Boolean:
                    return IntrinsicType.Boolean;
                case DatabasePrimitive.Double:
                    return IntrinsicType.Double;
                case DatabasePrimitive.Single:
                    return IntrinsicType.Single;
                case DatabasePrimitive.Byte:
                    return IntrinsicType.Byte;
                case DatabasePrimitive.Int16:
                    return IntrinsicType.Int16;
                case DatabasePrimitive.Int32:
                    return IntrinsicType.Int32;
                case DatabasePrimitive.Int64:
                    return IntrinsicType.Int64;
                case DatabasePrimitive.SByte:
                    return IntrinsicType.SByte;
                case DatabasePrimitive.UInt16:
                    return IntrinsicType.UInt16;
                case DatabasePrimitive.UInt32:
                    return IntrinsicType.UInt32;
                case DatabasePrimitive.UInt64:
                    return IntrinsicType.UInt64;
                case DatabasePrimitive.String:
                    return IntrinsicType.String;
                default:
                    throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, string.Format("Cannot map this database primitive to a CLR intrinsic: {0}.", primitive));
            }
        }
        #endregion


        /// <summary>
        /// Determines which method of the <see cref="DbState" /> class should be called to retrieve or store the value
        /// of database fields, and which type conversion is necessary.
        /// </summary>
        /// <param name="fieldType">Type of the field or the replacing property, as viewed by user code.</param>
        /// <param name="databaseAttribute"><see cref="DatabaseAttribute" /> for which the method
        /// is requested.</param>
        /// <param name="operation">Operation (<see cref="readOperation" /> or <see cref="writeOperation" />)</param>
        /// <param name="methodPair">Filled with the <see cref="DbState" /> method, and the</param> type from / to
        /// which the value returned by / passed to <paramref name="method" /> has to be casted (or <b>null</b> if no
        /// cast is necessary).
        /// </param>
        private void GetMethod(
            ITypeSignature fieldType,
            DatabaseAttribute databaseAttribute,
            string operation,
            out MethodCastPair methodPair) {

            MethodCastPair methodCastPair;
            IMethod method;
            ITypeSignature castType;
            string methodName;

            string key = operation + " : " + GetAttributeTypeCacheKey(databaseAttribute);
            if (this.cache.TryGetValue(key, out methodCastPair)) {
                methodPair = methodCastPair;
                return;
            }

            castType = null;
            methodName = null;

            Func<Type, string, MethodInfo> DoFindMethodByName = (stateType, name) => {
                MethodInfo info;
                info = stateType.GetMethod(name);
                Trace.Assert(info != null, "Missing method " + stateType.Name + "." + name);
                Trace.Assert(
                    name.StartsWith("Read") && info.GetParameters().Length == 3 ||
                    name.StartsWith("Write") && info.GetParameters().Length == 4,
                    "Errornous, legacy signature of " + stateType.Name + "." + name
                    );
                return info;
            };
 
            DatabasePrimitiveType primitiveType;
            DatabaseEnumType enumType = null;
            if ((primitiveType = databaseAttribute.AttributeType as DatabasePrimitiveType) != null ||
                (enumType = databaseAttribute.AttributeType as DatabaseEnumType) != null) {
                DatabasePrimitive primitive = 
                    primitiveType != null ? primitiveType.Primitive : enumType.UnderlyingType;

                if (databaseAttribute.IsNullable) {
                    methodName = operation + "Nullable" + primitive.ToString();
                    if (enumType != null) {
                        // The caller will have to cast to/from Nullable<primitive>.
                        // Note that this is a non-trivial cast.
                        castType = new GenericTypeInstanceTypeSignature(
                            (INamedType)
                            this.module.FindType(typeof(Nullable<>), BindingOptions.RequireGenericDefinition),
                            new ITypeSignature[] { this.module.Cache.GetIntrinsic(MapDatabasePrimitiveToInstrinsic(primitive)) });
                    }
                } else if (databaseAttribute.IsTypeName) {
                    methodName = operation + "TypeName";
                } else {
                    methodName = operation + primitive.ToString();
                }

                MethodInfo methodInfo;

                methodInfo = codeGeneratedDbStateType == null ? null : codeGeneratedDbStateType.GetMethod(methodName);
                if (methodInfo == null) {
                    methodInfo = DoFindMethodByName(dbStateType, methodName);
                }

                Trace.Assert(methodInfo != null, string.Format("Cannot find the method DbState.{0}.", methodName));

                method = this.module.FindMethod(methodInfo, BindingOptions.Default);

            } else if (databaseAttribute.AttributeType is DatabaseArrayType) {
                methodName = operation + "Array";
                if (operation == readOperation) {
                    method =
                    this.GetGenericMethodInstance(methodName,
                    ((ArrayTypeSignature)
                    TypeSpecDeclaration.Unwrap(fieldType)).
                    ElementType);
                } else {
                    MethodInfo methodInfo = DoFindMethodByName(dbStateType, methodName);
                    Trace.Assert(methodInfo != null, string.Format("Cannot find the method DbState.{0}.", methodName));
                    method = this.module.FindMethod(methodInfo, BindingOptions.Default);
                }
            } else if (databaseAttribute.AttributeType is DatabaseClass) {
                methodName = operation + "Object";
                if (databaseAttribute.IsTypeReference) {
                    methodName = operation + "TypeReference";
                }
                else if (databaseAttribute.IsInheritsReference) {
                    methodName = operation + "Inherits";
                }

                MethodInfo methodInfo;

                methodInfo = codeGeneratedDbStateType == null ? null : codeGeneratedDbStateType.GetMethod(methodName);
                if (methodInfo == null) {
                    methodInfo = DoFindMethodByName(dbStateType, methodName);
                }
                Trace.Assert(methodInfo != null, string.Format("Cannot find the method DbState.{0}.", methodName));
                method = this.module.FindMethod(methodInfo, BindingOptions.Default);
                // The caller will have to cast from this type:
                castType = this.module.Cache.GetType(typeof(IObjectView));
            } else {
                Trace.Assert(false, string.Format("Not an expected type: {0}", databaseAttribute.AttributeType));
                method = null;
            }

            var setValueCallback = SetValueCallbackFacadeType.GetMethod(methodName);
            
            methodCastPair = new MethodCastPair();
            methodCastPair.Method = method;
            methodCastPair.CastType = castType;
            methodCastPair.SetValueCallbackMethod = setValueCallback == null ? 
                null : this.module.FindMethod(setValueCallback, BindingOptions.Default);
            
            this.cache.Add(key, methodCastPair);
            methodPair = methodCastPair;
        }

        MethodInfo DoGetAccessMethodByName(Type stateType, string methodName) {
            return stateType.GetMethod(methodName);
        }

        /// <summary>
        /// Determines which method of the <see cref="DbState" /> class should be called to retrieve
        /// the value
        /// of database fields, and which type conversion is necessary.
        /// </summary>
        /// <param name="fieldType">Type of the field or the replacing property, as viewed by user code.</param>
        /// <param name="databaseAttribute"><see cref="DatabaseAttribute" /> for which the method
        /// is requested.</param>
        /// <param name="getMethod">Filled with the <see cref="DbState" /> method.</param>
        /// <param name="castType">Filled with the type from the value returned by
        /// <paramref name="getMethod" /> has to be casted, or <b>null</b> if no cast is necessary.</param>
        /// <returns><b>true</b> if the method exist, otherwise <b>false</b>.</returns>
        public bool GetGetMethod(ITypeSignature fieldType, DatabaseAttribute databaseAttribute,
        out IMethod getMethod, out ITypeSignature castType) {
            MethodCastPair pair;
            GetMethod(fieldType, databaseAttribute, readOperation, out pair);
            getMethod = pair.Method;
            castType = pair.CastType;
            return true;
        }

        /// <summary>
        /// Determines which method of the <see cref="DbState" /> class should be called to store the value
        /// of database fields, and which type conversion is necessary.
        /// </summary>
        /// <param name="fieldType">Type of the field or the replacing property, as viewed by user code.</param>
        /// <param name="databaseAttribute"><see cref="DatabaseAttribute" /> for which the method
        /// is requested.</param>
        /// <param name="setMethod">Filled with the <see cref="DbState" /> method.</param>
        /// <param name="castType">Filled with the type to which the value passed to
        /// <paramref name="setMethod" /> has to be casted, or <b>null</b> if no cast is necessary.</param>
        /// <param name="setValueCallback">The corrsponding infrastructure set value callback
        /// invocation method that are to be called if the <see cref="ISetValueCallback"/> interface
        /// is implemented on the database class about to be transformed.</param>
        /// <returns><b>true</b> if the method exist, otherwise <b>false</b> (happens for instance
        /// when the <b>write</b> operation is requested on an intrinsically read-only type).</returns>
        public bool GetSetMethod(ITypeSignature fieldType, DatabaseAttribute databaseAttribute,
        out IMethod setMethod, out ITypeSignature castType, out IMethod setValueCallback) {
            MethodCastPair pair;
            GetMethod(fieldType, databaseAttribute, writeOperation, out pair);
            setMethod = pair.Method;
            castType = pair.CastType;
            setValueCallback = pair.SetValueCallbackMethod;
            return true;
        }

        /// <summary>
        /// Tries the name of the get generated method by.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="method">The method.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool TryGetGeneratedMethodByName(string name, out IMethod method) {
            MethodInfo generatedMethod;

            method = null;

            if (codeGeneratedDbStateType == null)
                return false;

            generatedMethod = codeGeneratedDbStateType.GetMethod(name);
            if (generatedMethod == null)
                return false;

            method = this.module.FindMethod(generatedMethod, BindingOptions.Default);
            return true;
        }
    }
}