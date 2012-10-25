// ***********************************************************************
// <copyright file="Master.json.g.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

// This is a system generated file. It reflects the Starcounter App Template defined in the file "unknown"
// DO NOT MODIFY DIRECTLY - CHANGES WILL BE OVERWRITTEN

using System;
using System.Collections.Generic;
using Starcounter;
using Starcounter.Internal;
using Starcounter.Templates;

/// <summary>
/// Class Master
/// </summary>
public partial class Master {
    /// <summary>
    /// The default template
    /// </summary>
    public static MasterTemplate DefaultTemplate = new MasterTemplate();
    /// <summary>
    /// Initializes a new instance of the <see cref="Master" /> class.
    /// </summary>
    public Master() {
        Template = DefaultTemplate;
    }
    /// <summary>
    /// The template defining the properties of this App.
    /// </summary>
    /// <value>The template.</value>
    public new MasterTemplate Template { get { return (MasterTemplate)base.Template; } set { base.Template = value; } }
    /// <summary>
    /// Here you can set properties for each property in this App (such as Editable, Visible and Enabled).
    /// The changes only affect this instance.
    /// If you which to change properties for the template, use the Template property instead.
    /// </summary>
    /// <value>The metadata.</value>
    /// <remarks>It is much less expensive to set this kind of metadata for the
    /// entire template (for example to mark a property for all App instances as Editable).</remarks>
    public new MasterMetadata Metadata { get { return (MasterMetadata)base.Metadata; } }
    /// <summary>
    /// Gets or sets the page.
    /// </summary>
    /// <value>The page.</value>
    public App Page { get { return GetValue<App>(Template.Page); } set { SetValue(Template.Page, value); } }
    /// <summary>
    /// Gets or sets the test.
    /// </summary>
    /// <value>The test.</value>
    public String Test { get { return GetValue(Template.Test); } set { SetValue(Template.Test, value); } }
    /// <summary>
    /// Class MasterTemplate
    /// </summary>
    public class MasterTemplate : AppTemplate {
        /// <summary>
        /// Initializes a new instance of the <see cref="MasterTemplate" /> class.
        /// </summary>
        public MasterTemplate()
            : base() {
            InstanceType = typeof(Master);
            ClassName = "Master";
            Page = Register<AppTemplate>("Page");
            Test = Register<StringProperty, string>("Test", true);
            Test.AddHandler( (App app, Property<string> prop, string value) => { return (new Input.Test() { App = (Master)app, Template = (StringProperty)prop, Value = value } ) ; }, (App app, Input<string> Input) => ((Master)app).Handle((Input.Test)Input) );
        }
        /// <summary>
        /// The page
        /// </summary>
        public AppTemplate Page;
        /// <summary>
        /// The test
        /// </summary>
        public StringProperty Test;
    }
    /// <summary>
    /// Class MasterMetadata
    /// </summary>
    public class MasterMetadata : AppMetadata {
        /// <summary>
        /// Initializes a new instance of the <see cref="MasterMetadata" /> class.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="template">The template.</param>
        public MasterMetadata(App app, AppTemplate template) : base(app, template) { }
        /// <summary>
        /// Gets the app.
        /// </summary>
        /// <value>The app.</value>
        public new Master App { get { return (Master)base.App; } }
        /// <summary>
        /// Gets the template.
        /// </summary>
        /// <value>The template.</value>
        public new Master.MasterTemplate Template { get { return (Master.MasterTemplate)base.Template; } }
        /// <summary>
        /// Gets the page.
        /// </summary>
        /// <value>The page.</value>
        public AppMetadata Page { get { return __p_Page ?? (__p_Page = new AppMetadata(App, App.Template.Page)); } } private AppMetadata __p_Page;
        /// <summary>
        /// Gets the test.
        /// </summary>
        /// <value>The test.</value>
        public StringMetadata Test { get { return __p_Test ?? (__p_Test = new StringMetadata(App, App.Template.Test)); } } private StringMetadata __p_Test;
    }
    /// <summary>
    /// Class Json
    /// </summary>
    public static class Json {
    }
    /// <summary>
    /// Class Input
    /// </summary>
    public static class Input {
        /// <summary>
        /// Class Test
        /// </summary>
        public class Test : Input<Master, StringProperty, String> {
        }
    }
}
