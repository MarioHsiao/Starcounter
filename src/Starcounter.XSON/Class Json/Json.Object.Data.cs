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
                return (IBindable)_data;
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
                return _data;
            }
            set {
                this.Scope<Json, object>((j, v) => {
                    if (j.IsArray) {
                        j._PendingEnumeration = true;
                        j._data = (IEnumerable)v;
                        j.Array_InitializeAfterImplicitConversion((Json)j.Parent, (TObjArr)j.Template);
                    } else {
                        if (j.Template == null) {
                            j.CreateDynamicTemplate(); // If there is no template, we'll create a template
                        }
                        j.InternalSetData(v, (TValue)j.Template, false);
                    }
                },
                this, value);
            }
        }

        internal void AttachData(object data) {
            InternalSetData(data, (TValue)Template, true);
        }
        
        /// <summary>
        /// Sets the underlying data object and refreshes all bound values.
        /// This function does not check for a valid transaction as the 
        /// public Data-property does.
        /// </summary>
        /// <param name="data">The bound data object (usually an Entity)</param>
        protected virtual void InternalSetData(object data, TValue template, bool updateBinding) {
            TObject tobj;
            TValue child;

            this._data = data;

            if (template == null)
                return;

            if (template.TemplateTypeId == TemplateTypeEnum.Object) {
                tobj = (TObject)template;

                // Since dataobject is set we want to do a reverse update of the binding,
                // i.e. if the template is bound we want to update the parents dataobject.
                InitTemplateAfterData(template, false);
                
                if (template.BindingStrategy != BindingStrategy.Unbound) {
                    var parent = ((Json)this.Parent);
                    if (!updateBinding && parent != null && template.UseBinding(parent)) {
                        if (tobj.BoundSetter != null)
                            tobj.BoundSetter(parent, data);
                    }
                }
                
                for (Int32 i = 0; i < tobj.Properties.Count; i++) {
                    child = tobj.Properties[i] as TValue;

                    if (child == null)
                        continue;

                    InitTemplateAfterData(child, true);
                }
            } else {
                InitTemplateAfterData(template, true);
            }
            
            OnData();
        }
        
        private void InitTemplateAfterData(TValue template, bool updateBinding) {
            if (template.BindingStrategy != BindingStrategy.Unbound) {
                if (_data != null) {
                    if (template.isVerifiedUnbound) {
                        template.isVerifiedUnbound = template.VerifyBoundDataType(this._data.GetType(), template.dataTypeForBinding);
                    }

                    if (updateBinding && (template.TemplateTypeId == TemplateTypeEnum.Object 
                                            || template.TemplateTypeId == TemplateTypeEnum.Array)) {
                        if (template.UseBinding(this))
                            Refresh(template);
                    }
                } else {
                    if (updateBinding && template.HasBinding()) {
                        if (template.TemplateTypeId == TemplateTypeEnum.Object) {
                            (((TObject)template).Getter(this)).Data = null;
                        } else {
                            // Template previously bound. Reset the unbound value in case
                            // the dirtycheck have used it to cache old value.
                            template.SetDefaultValue(this);
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
            if (_transaction != TransactionHandle.Invalid)
                return _transaction;

            if (lookInStepSiblings == true && _stepSiblings != null) {
                foreach (Json stepSibling in _stepSiblings) {
                    if (stepSibling == this)
                        continue;
                    handle = stepSibling.GetTransactionHandle(false);
                    if (handle != TransactionHandle.Invalid)
                        return handle;
                }
            }

            if (_parent != null)
                return _parent.GetTransactionHandle(true);

            return TransactionHandle.Invalid;
        }

        public void AttachCurrentTransaction() {
            if (StarcounterBase.TransactionManager != null) {
                var current = StarcounterBase.TransactionManager.CurrentTransaction;
                if (current != TransactionHandle.Invalid && !current.IsImplicit) {
                    if (this.isAddedToViewmodel) {
                        // Attach a transaction to a jsonobject already added to a Viewmodel (Session).
                        // Need to register a reference directly here and properly deregister any existing 
                        // transaction.
                        var session = Session;
                        if (_transaction != TransactionHandle.Invalid)
                            session.DeregisterTransaction(_transaction);
                        current = session.RegisterTransaction(current);
                    }

                    _transaction = current;
                    StarcounterBase.TransactionManager.SetTemporaryRef(current);
                }
            }
        }
    }
}