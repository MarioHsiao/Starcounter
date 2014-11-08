﻿using System;
using Starcounter.Templates;

namespace Starcounter {
    // Not the right place for this class, but a temporary location to add a baseclass for
    // polyjuice pages (with automatic return of mimetype html) and access to X.Get().
    // Should be moved to a separate project and included in the project templates used in extension
    // for VS.
    public class Page : Json {
        public static Page.JsonByExample.Schema DefaultTemplate = new Page.JsonByExample.Schema();
        public Page() { }
        public Page(JsonByExample.Schema template) { Template = template; }
        protected override Template GetDefaultTemplate() { return DefaultTemplate; }
        public new JsonByExample.Schema Template { get { return (JsonByExample.Schema)base.Template; } set { base.Template = value; } }
        public override bool IsCodegenerated { get { return true; } }

        private String __bf__Html__;

        public new static class JsonByExample {
            public class Schema : Json.JsonByExample.Schema {
                public Schema()
                    : base() {
                    InstanceType = typeof(Page);
                    ClassName = "Page";
                    Properties.ClearExposed();
                    Html = Add<TString>("Html");
                    Html.SetCustomAccessors((json) => { return ((Page)json).__bf__Html__; }, (json, value) => { ((Page)json).__bf__Html__ = (string)value; }, false);
                }
                public override object CreateInstance(Json parent) { return new Page(this) { Parent = parent }; }
                public TString Html;
            }
        }

        public string Html {
            get { return Template.Html.Getter(this); }
            set { Template.Html.Setter(this, value); }
        }

        public override string AsMimeType(MimeType mimeType) {
            if (mimeType == MimeType.Text_Html) {
                return (string)X.GET(Html);
            }
            return base.AsMimeType(mimeType);
        }

        public override string GetHtmlPartialUrl() {
            return Html;
        }
    }
}