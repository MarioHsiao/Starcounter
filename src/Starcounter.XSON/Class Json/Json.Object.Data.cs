// ***********************************************************************
// <copyright file="Obj.IBindable.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Templates;
using Starcounter.Advanced;
using Starcounter.Advanced.XSON;
using System.Collections;
using Starcounter.Internal.XSON;
using Starcounter.Internal;
using Starcounter.XSON;

namespace Starcounter {
    partial class Json {
        /// <summary>
        /// 
        /// </summary>
        internal IBindable DataAsBindable {
            get {
                return (IBindable)this.data;
            }
        }

        /// <summary>
        /// Gets or sets the bound (underlying) data object (often a database Entity object). This enables
        /// the Obj to reflect the values of the bound object. The values are matched by property names by default.
        /// When you declare an Obj using generics, be sure to specify the type of the bound object in the class declaration.
        /// </summary>
        /// <value>The bound data object (often a database Entity)</value>
        public object Data {
            get {
                return this.data;
            }
            set {
                this.Scope<Json, object>((j, v) => {
                    if (j.IsArray) {
                        ((IList)this).Clear(); // Clear any existing items since a new dataobject is set.
                        j.pendingEnumeration = true;
                        j.data = (IEnumerable)v;
                        j.Array_InitializeAfterImplicitConversion((Json)j.Parent, (TObjArr)j.Template);
                    } else {
                        if (j.Template == null) {
                            j.CreateDynamicTemplate(v); // If there is no template, we'll create a template
                        }
                        j.InternalSetData(v, (TValue)j.Template, true);
                    }
                },
                this, value);
            }
        }

        internal void AttachData(object data, bool updateParentBinding) {
            InternalSetData(data, (TValue)Template, updateParentBinding);
        }
        
        /// <summary>
        /// Sets the underlying data object and refreshes all bound values.
        /// This function does not check for a valid transaction as the 
        /// public Data-property does.
        /// </summary>
        /// <param name="data">The bound data object (usually an Entity)</param>
        protected virtual void InternalSetData(object data, TValue template, bool updateParentBinding) {
            TObject tobj;
            TValue child;

            this.data = data;

            if (template == null)
                return;

            if (template.TemplateTypeId == TemplateTypeEnum.Object) {
                tobj = (TObject)template;

                // Since dataobject is set we want to do a reverse update of the binding,
                // i.e. if the template is bound we want to update the parents dataobject.
//                InitTemplateAfterData(data, template, updateBinding);
                
                if (template.BindingStrategy != BindingStrategy.Unbound) {
                    var parent = ((Json)this.Parent);
                    if (updateParentBinding && parent != null && template.UseBinding(parent)) {
                        if (tobj.BoundSetter != null)
                            tobj.BoundSetter(parent, data);
                    }
                }
                
                for (Int32 i = 0; i < tobj.Properties.Count; i++) {
                    child = tobj.Properties[i] as TValue;

                    if (child == null)
                        continue;

                    InitTemplateAfterData(data, child);
                }
            } else {
                InitTemplateAfterData(data, template);
            }
            
            OnData();
        }
        
        private void InitTemplateAfterData(object data, TValue template) {
            if (template.BindingStrategy != BindingStrategy.Unbound) {
                if (data != null) {
                    if (template.isVerifiedUnbound) {
                        if (!template.VerifyBoundDataType(data.GetType(), template.dataTypeForBinding))
                            template.InvalidateBoundGetterAndSetter();
                    }
                } else {
                    if (template.HasBinding()) {
                        if (template.TemplateTypeId == TemplateTypeEnum.Object) {
                            (((TObject)template).Getter(this)).Data = null;
                        } else {
                            // Template previously bound. Reset the unbound value in case
                            // the dirtycheck have used it to cache old value.
                            template.SetDefaultValue(this, true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called after the Data property is set.
        /// </summary>
        protected virtual void OnData() {
        }

        /// <summary>
        /// Gets the nearest transaction.
        /// </summary>
        public ITransaction Transaction {
            get {
                var handle = GetTransactionHandle(true);
                if (handle != TransactionHandle.Invalid)
                    return StarcounterBase.TransactionManager.WrapHandle(handle);
                return null;
            }
        }

        internal TransactionHandle GetTransactionHandle(bool lookInStepSiblings) {
            TransactionHandle handle;

            // Returning first available transaction climbing up the tree starting from this node.
            if (this.transaction != TransactionHandle.Invalid)
                return this.transaction;

            if (lookInStepSiblings == true && this.siblings != null) {
                foreach (Json stepSibling in this.siblings) {
                    if (stepSibling == this)
                        continue;
                    handle = stepSibling.GetTransactionHandle(false);
                    if (handle != TransactionHandle.Invalid)
                        return handle;
                }
            }

            if (this.parent != null)
                return this.parent.GetTransactionHandle(true);

            return TransactionHandle.Invalid;
        }

        public void AttachCurrentTransaction() {
            if (StarcounterBase.TransactionManager != null) {
                var current = StarcounterBase.TransactionManager.CurrentTransaction;
                if (current != TransactionHandle.Invalid && !current.IsImplicit) {
                    if (this.isStateful) {
                        // Attach a transaction to a jsonobject already added to a Viewmodel (Session).
                        // Need to register a reference directly here and properly deregister any existing 
                        // transaction.
                        var session = Session;
                        if (this.transaction != TransactionHandle.Invalid)
                            session.DeregisterTransaction(this.transaction);
                        current = session.RegisterTransaction(current);
                    }

                    this.transaction = current;
                    StarcounterBase.TransactionManager.SetTemporaryRef(current);
                }
            }
        }
    }
}