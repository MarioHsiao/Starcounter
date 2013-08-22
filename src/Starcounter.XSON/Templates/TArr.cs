﻿// ***********************************************************************
// <copyright file="TArr.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using Starcounter.Advanced;
using Starcounter.Advanced.XSON;
using Starcounter.Internal.XSON;

namespace Starcounter.Templates {

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="OT"></typeparam>
    /// <typeparam name="OTT"></typeparam>
    public class TArr<OT,OTT> : TObjArr
        where OT : Obj, new()
        where OTT : TObj
    {
        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>System.Object.</returns>
        public override object CreateInstance(Container parent) {
            return new Arr<OT>((Obj)parent, this);
        }

        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override System.Type InstanceType {
            get { return typeof(Arr<OT>); }
        }

        public override void ProcessInput(Obj obj, byte[] rawValue) {
            throw new System.NotImplementedException();
        }


        /// <summary>
        /// Instructs the array what object template should be used for each element
        /// in this object array.
        /// </summary>
        /// <value>The app.</value>
        public override TObj ElementType {
            get {
                if (_Single.Length == 0)
                    return null;
                return (TObj)_Single[0];
            }
            set {
                if (InstanceDataTypeName != null)
                    value.InstanceDataTypeName = InstanceDataTypeName;
                _Single = new OTT[1];
                _Single[0] = (OTT)value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal OTT[] _Single = new OTT[0];


        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <value>The children.</value>
        public override IEnumerable<Template> Children {
            get {
                return (IEnumerable<Template>)_Single;
            }
        }

        protected override IReadOnlyList<Internal.IReadOnlyTree> _Children {
            get { return _Single; }
        }


    }

    /// <summary>
    /// Class TArr
    /// </summary>
    public abstract class TObjArr : TContainer
#if IAPP
//        , ITObjArr
#endif
    {
       // internal DataValueBinding<IEnumerable> dataBinding;
        private string instanceDataTypeName;
    
        /// <summary>
        /// Gets or sets the type (the template) that should be the template for all elements
        /// in this array.
        /// </summary>
        /// <value>The obj template adhering to each element in this array</value>
        public abstract TObj ElementType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string InstanceDataTypeName {
            get { return instanceDataTypeName; }
            set {
                TObj app = ElementType;
                if (app != null)
                    app.InstanceDataTypeName = value;
                instanceDataTypeName = value;
            }
        }

        /// <summary>
        /// Contains the default value for the property represented by this
        /// Template for each new App object.
        /// </summary>
        /// <value>The default value as object.</value>
        /// <exception cref="System.NotImplementedException"></exception>
        public override object DefaultValueAsObject {
            get {
                throw new System.NotImplementedException();
            }
            set {
                throw new System.NotImplementedException();
            }
        }

        internal override bool UseBinding(IBindable data) {
			if (data == null)
				return false;
            return DataBindingFactory.VerifyOrCreateBinding(this, data.GetType());
        }

        internal override object GetBoundValueAsObject(Obj obj) {
            throw new NotImplementedException();
        }

        internal override void SetBoundValueAsObject(Obj obj, object value) {
            throw new NotImplementedException();
        }
    }

}
