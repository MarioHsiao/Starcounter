using EnvDTE;
using EnvDTE80;
using System.IO;

namespace Starcounter.VisualStudio
{
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
	internal class AppsEvents
	{
        private ProjectItemsEvents projectEvents;
        private DocumentEvents documentEvents;

        /// <summary>
        /// Registers handlers for all needed events.
        /// </summary>
        /// <param name="package"></param>
		internal void AddEventListeners(VsPackage package)
		{
            Events2 e2 = (Events2)package.DTE.Events;

            if (this.projectEvents != null) {
                this.projectEvents.ItemRenamed -= ProjectItemsEvents_ItemRenamed;
            }
            this.projectEvents = e2.ProjectItemsEvents;
            this.projectEvents.ItemRenamed += ProjectItemsEvents_ItemRenamed;

            if (this.documentEvents != null) {
                this.documentEvents.DocumentSaved -= DocumentEvents_DocumentSaved;
            }
            this.documentEvents = e2.DocumentEvents;
            this.documentEvents.DocumentSaved += DocumentEvents_DocumentSaved;
		}

        /// <summary>
        /// Removes all registered eventslisteners.
        /// </summary>
        internal void RemoveEventListeners() {
            if (projectEvents != null) {
                projectEvents.ItemRenamed -= ProjectItemsEvents_ItemRenamed;
                projectEvents = null;
            }

            if (documentEvents != null) {
                documentEvents.DocumentSaved -= DocumentEvents_DocumentSaved;
                documentEvents = null;
            }
        }

        private void DocumentEvents_DocumentSaved(Document document) {
            ProjectItem parentItem;
            ProjectItem projectItem = document.ProjectItem;
            Window windowForParent = null;

            if (projectItem == null)
                return;

            parentItem = projectItem.Collection.Parent as ProjectItem;
            if ((parentItem != null) && parentItem.Name.EndsWith(".json")) {
                if (!parentItem.IsOpen) {
                    // If the json file is not open in the studio we need to open it before we can save it.
                    windowForParent = parentItem.Open();
                }

                parentItem.Save();

                if (windowForParent != null)
                    windowForParent.Close();
            }
        }

        private void ProjectItemsEvents_ItemRenamed(ProjectItem projectItem, string oldName)
        {
            string extension;
            string newFileName;

            if (oldName.EndsWith(".json"))
            {
                newFileName = projectItem.Name;
                foreach (ProjectItem pi in projectItem.ProjectItems)
                {
                    extension = Path.GetExtension(pi.Name);
                    pi.Name = newFileName + extension;
                }
            }
        }
	}
}