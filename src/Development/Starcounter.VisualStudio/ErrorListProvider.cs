
using Microsoft.VisualStudio.Shell;
using Starcounter.Internal;
using System;
using System.Collections.Generic;

namespace Starcounter.VisualStudio {
    /// <summary>
    /// Known sources that can create tasks to be put in the
    /// error list.
    /// </summary>
    internal enum ErrorTaskSource {
        Debug,
        Other
    }

    /// <summary>
    /// Implements the specialized <see cref="ErrorListProvider"/> we use to
    /// report about deployment and debugging problems.
    /// </summary>
    internal class StarcounterErrorListProvider : ErrorListProvider {
        /// <summary>
        /// Gets the package to which this provider belongs.
        /// </summary>
        public readonly VsPackage Package;

        /// <summary>
        /// Initializes a new <see cref="StarcounterErrorListProvider"/>.
        /// </summary>
        /// <param name="package"></param>
        public StarcounterErrorListProvider(VsPackage package)
            : base(package) {
            this.Package = package;
        }

        /// <summary>
        /// Creates a new error task. The task is not stored in the
        /// tasks. collection of this <see cref="ErrorListProvider"/>.
        /// Use Tasks.Add to add the created task to
        /// the list of errors to be displayed in the GUI.
        /// </summary>
        /// <param name="source">The source of the new task item.</param>
        /// <param name="text">The text to assign the error task.</param>
        /// <param name="code">A Starcounter error code to attach to the task.</param>
        /// <returns>A task item.</returns>
        public StarcounterErrorTask NewTask(ErrorTaskSource source, string text, uint code = Error.SCERRUNSPECIFIED) {
            var task = new StarcounterErrorTask(text, code);
            SetTaskDefaults(task, source);
            return task;
        }

        /// <summary>
        /// Creates a new error task. The task is not stored in the
        /// tasks collection of this <see cref="ErrorListProvider"/>.
        /// Use Tasks.Add to add the created task to
        /// the list of errors to be displayed in the GUI.
        /// </summary>
        /// <param name="source">The source of the new task item.</param>
        /// <param name="exception">
        /// An exception whose data we should use to populate the task.
        /// </param>
        /// <returns>A task item.</returns>
        public StarcounterErrorTask NewTask(ErrorTaskSource source, Exception exception) {
            var task = new StarcounterErrorTask(exception);
            SetTaskDefaults(task, source);
            return task;
        }

        /// <summary>
        /// Creates a new error task. The task is not stored in the
        /// Tasks collection of this <see cref="ErrorListProvider"/>.
        /// Use Tasks.Add to add the created task to
        /// the list of errors to be displayed in the GUI.
        /// </summary>
        /// <param name="source">The source of the new task item.</param>
        /// <param name="message">
        /// An <see cref="ErrorMessage"/> whose data we should use to populate
        /// the task.
        /// </param>
        /// <returns>A task item.</returns>
        public StarcounterErrorTask NewTask(ErrorTaskSource source, ErrorMessage message) {
            var task = new StarcounterErrorTask(message);
            SetTaskDefaults(task, source);
            return task;
        }

        /// <summary>
        /// Clears the underlying task item collection from all tasks
        /// added using this provider. To clear the full set of tasks,
        /// no matter of provider, use Tasks.Clear.
        /// </summary>
        public void Clear() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clears the underlying task item collection from all tasks
        /// added using this provider matching the given source. To
        /// clear the full set of tasks, no matter of provider,
        /// use Tasks.Clear.
        /// </summary>
        public void Clear(ErrorTaskSource source) {
            int subcategory;
            StarcounterErrorTask candidate;
            List<StarcounterErrorTask> tasksToRemove;

            subcategory = GetSubcategoryIndex(source);
            tasksToRemove = null;

            foreach (Task task in this.Tasks) {
                candidate = task as StarcounterErrorTask;
                if (candidate == null)
                    continue;

                if (candidate.SubcategoryIndex == subcategory) {
                    if (tasksToRemove == null)
                        tasksToRemove = new List<StarcounterErrorTask>();
                    tasksToRemove.Add(candidate);
                }
            }

            if (tasksToRemove != null) {
                foreach (var item in tasksToRemove) {
                    try {
                        this.Tasks.Remove(item);
                    } catch {
                        // Ingore possible errors removing.
                    }
                }
            }
        }

        private int GetSubcategoryIndex(ErrorTaskSource source) {
            int index;
            string name;

            name = "Starcounter" + Enum.GetName(typeof(ErrorTaskSource), source);
            index = this.Subcategories.IndexOf(name);
            return index == -1
                ? this.Subcategories.Add(name)
                : index;
        }

        private void SetTaskDefaults(StarcounterErrorTask task, ErrorTaskSource source) {
            task.SubcategoryIndex = GetSubcategoryIndex(source);
            task.Package = this.Package;
            task.Category = TaskCategory.All;
            task.CanDelete = true;
        }
    }
}