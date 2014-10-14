using EnvDTE;
using EnvDTE80;
using System;
using System.IO;
using System.Linq;

namespace Starcounter.VisualStudio {
    /// <summary>
    /// This class contains some extra functionality needed for Apps Exe projects.
    /// Currently it does two things:
    /// 
    /// Handles renaming of the json file and sub-files. When the jsonfile is renamed
    /// all child-items (currently the code-behind file) are renamed as well.
    /// 
    /// When the codebehind file is saved the buildtask that generates code is also triggered.
    /// This is needed since the generated code uses information from both the json file and 
    /// the codebehind file.
    /// </summary>
    /// <remarks>
    /// For this to work properly the json file and codebehind file MUST be grouped together 
    /// with the json file as root.
    /// </remarks>
    internal class AppsEvents {
        private const string PROPERTY_ITEMTYPE= "ItemType";
        private const string PROPERTY_CUSTOMTOOL = "CustomTool";
        private const string PROPERTY_FULLPATH = "FullPath";
        private const string VALUE_TYPEDJSON = "TypedJSON";
        private const string VALUE_COMPILE = "MsBuild:Compile";

        private ProjectItemsEvents projectEvents;
        private DocumentEvents documentEvents;
        DebuggerProcessEvents debuggerProcessEvents;

        /// <summary>
        /// Internally used callback that is invoked by the extension
        /// when it detects a change in debugger state. The arguments
        /// passed is the process ID, the name of the process and a
        /// boolean indicating if the debugger was attached (TRUE if
        /// detached; otherwise FALSE, indicating attached.
        /// </summary>
        internal static Action<int, string, bool> OnDebuggerProcessChange;

        /// <summary>
        /// Registers handlers for all needed events.
        /// </summary>
        /// <param name="package"></param>
        internal void AddEventListeners(VsPackage package) {
            Events2 e2 = (Events2)package.DTE.Events;

            if (this.projectEvents != null) {
                this.projectEvents.ItemRenamed -= ProjectItemsEvents_ItemRenamed;
                this.projectEvents.ItemAdded -= ProjectItemsEvents_ItemAdded;
            }
            this.projectEvents = e2.ProjectItemsEvents;
            this.projectEvents.ItemRenamed += ProjectItemsEvents_ItemRenamed;
            this.projectEvents.ItemAdded += ProjectItemsEvents_ItemAdded;

            if (this.documentEvents != null) {
                this.documentEvents.DocumentSaved -= DocumentEvents_DocumentSaved;
            }
            this.documentEvents = e2.DocumentEvents;
            this.documentEvents.DocumentSaved += DocumentEvents_DocumentSaved;

            if (this.debuggerProcessEvents != null) {
                this.debuggerProcessEvents.OnProcessStateChanged -= debuggerEvents_OnProcessStateChanged;
            }

            this.debuggerProcessEvents = e2.DebuggerProcessEvents;
            debuggerProcessEvents.OnProcessStateChanged += debuggerEvents_OnProcessStateChanged;
        }

        void debuggerEvents_OnProcessStateChanged(Process process, dbgProcessState processState) {
            if (OnDebuggerProcessChange != null) {
                OnDebuggerProcessChange(process.ProcessID, process.Name, processState == dbgProcessState.dbgProcessStateStop);
            }
        }

        /// <summary>
        /// Removes all registered eventslisteners.
        /// </summary>
        internal void RemoveEventListeners() {
            if (projectEvents != null) {
                projectEvents.ItemRenamed -= ProjectItemsEvents_ItemRenamed;
                projectEvents.ItemAdded -= ProjectItemsEvents_ItemAdded;
                projectEvents = null;
            }

            if (documentEvents != null) {
                documentEvents.DocumentSaved -= DocumentEvents_DocumentSaved;
                documentEvents = null;
            }

            if (debuggerProcessEvents != null) {
                debuggerProcessEvents.OnProcessStateChanged -= debuggerEvents_OnProcessStateChanged;
                debuggerProcessEvents = null;
            }
        }

        private void DocumentEvents_DocumentSaved(Document document) {
            ProjectItem parentItem;
            ProjectItem projectItem = document.ProjectItem;
            Window windowForParent = null;

            if (projectItem == null)
                return;

            parentItem = projectItem.Collection.Parent as ProjectItem;
            if ((parentItem != null) && parentItem.Name.EndsWith(".json", StringComparison.CurrentCultureIgnoreCase)) {
                if (!parentItem.IsOpen) {
                    // If the json file is not open in the studio we need to open it before we can save it.
                    windowForParent = parentItem.Open();
                }

                parentItem.Save();

                if (windowForParent != null)
                    windowForParent.Close();
            }
        }

        private void ProjectItemsEvents_ItemRenamed(ProjectItem projectItem, string oldName) {
            string extension;
            string newFileName;

            if (oldName.EndsWith(".json", StringComparison.CurrentCultureIgnoreCase)) {
                newFileName = projectItem.Name;
                foreach (ProjectItem pi in projectItem.ProjectItems) {
                    extension = Path.GetExtension(pi.Name);
                    pi.Name = newFileName + extension;
                }
            }
        }

        private void ProjectItemsEvents_ItemAdded(ProjectItem projectItem) {
            Property itemProperty;
            ProjectItem otherItem;
            string filename = projectItem.Name;

            if (filename.EndsWith(".json", StringComparison.CurrentCultureIgnoreCase)) {
                // A json file added. Lets set buildtype and custom tool for building.
                itemProperty = FindProperty(projectItem.Properties, PROPERTY_CUSTOMTOOL);
                if (itemProperty != null) 
                    itemProperty.Value = VALUE_COMPILE;
                itemProperty = FindProperty(projectItem.Properties, PROPERTY_ITEMTYPE);
                if (itemProperty != null) 
                    itemProperty.Value = VALUE_TYPEDJSON;

                // If a codebehind file already exist in the project we nest it under the jsonfile.
                otherItem = FindItem(projectItem.Collection.Parent, filename + ".cs");
                if (otherItem != null) {
                    ConnectProjectItems(projectItem, otherItem);
                }
            } else if (filename.EndsWith(".json.cs", StringComparison.CurrentCultureIgnoreCase)) {
                // A codebehind file. Find projectitem for json (if it exists) and connect them.
                otherItem = FindItem(projectItem.Collection.Parent, Path.GetFileNameWithoutExtension(filename));
                if (otherItem != null) {
                    ConnectProjectItems(otherItem, projectItem);
                }
            }
        }

        private ProjectItem FindItem(object parent, string nameWOExt) {
            ProjectItems items;

            if (parent is ProjectItem) {
                items = ((ProjectItem)parent).ProjectItems;
            } else if (parent is Project) {
                items = ((Project)parent).ProjectItems;
            } else {
                return null;
            }

            foreach (ProjectItem pi in items) {
                if (nameWOExt.Equals(pi.Name))
                    return pi;
            }
            return null;
        }

        private Property FindProperty(EnvDTE.Properties properties, string propertyName) {
            foreach (Property p in properties) {
                if (propertyName.Equals(p.Name))
                    return p;
            }
            return null;
        }

        private void ConnectProjectItems(ProjectItem root, ProjectItem child) {
            Property itemProperty = FindProperty(child.Properties, PROPERTY_FULLPATH);
            if (itemProperty != null) {
                root.ProjectItems.AddFromFile((string)itemProperty.Value);
                root.ExpandView();
            }
        }
    }
}