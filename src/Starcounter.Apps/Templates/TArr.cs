// ***********************************************************************
// <copyright file="TArr.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using Starcounter.Templates.DataBinding;

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

        internal override void ProcessInput(Obj obj, byte[] rawValue) {
            throw new System.NotImplementedException();
        }


        /// <summary>
        /// Gets or sets the app.
        /// </summary>
        /// <value>The app.</value>
        public override TObj App {
            get {
                if (_Single.Length == 0)
                    return null;
                return (TObj)_Single[0];
            }
            set {
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


    }

    /// <summary>
    /// Class TArr
    /// </summary>
    public abstract class TObjArr : TContainer
#if IAPP
//        , ITObjArr
#endif
    {

        private DataBinding<Rows> dataBinding;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataGetter"></param>
        public void AddDataBinding(Func<Obj, Rows> dataGetter) {
            dataBinding = new DataBinding<Rows>(dataGetter);
            Bound = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public Rows GetBoundValue(Obj app) {
            return dataBinding.GetValue(app);
        }
        
        /// <summary>
        /// Gets or sets the type (the template) that should be the template for all elements
        /// in this array.
        /// </summary>
        /// <value>The obj template adhering to each element in this array</value>
        public abstract TObj App { get; set; }

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

    }

}
