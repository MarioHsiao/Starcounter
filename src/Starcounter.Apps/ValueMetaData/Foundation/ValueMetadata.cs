

namespace Starcounter.Templates {
    public class ValueMetadata : AppMetadataBase {
        public ValueMetadata(App app, Template template ) : base( app, template ) {
        }

        public bool Enabled {
            set;
            get;
        }
        public bool Visible {
            set;
            get;
        }
        public bool Editable {
            set;
            get;
        }
    }
}
