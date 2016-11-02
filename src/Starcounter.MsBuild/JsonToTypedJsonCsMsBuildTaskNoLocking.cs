using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SXP = Starcounter.XSON.PartialClassGenerator;

namespace Starcounter.Internal.MsBuild {
    /// <summary>
    /// Task for MsBuild responsible for generating partial class for TypedJSON. 
    /// Executed without loading into domain (slower).
    /// </summary>
    public class JsonToTypedJsonCsMsBuildTaskNoLocking : AppDomainIsolatedTask {
        static JsonToTypedJsonCsMsBuildTaskNoLocking() {
            Bootstrapper.Bootstrap();
        }

        /// <summary>
        /// Gets or sets the input files.
        /// </summary>
        /// <value>The input files.</value>
        [Required]
        public ITaskItem[] InputFiles { get; set; }

        /// <summary>
        /// Gets or sets the output files.
        /// </summary>
        /// <value>The output files.</value>
        [Output]
        public ITaskItem[] OutputFiles { get; set; }

        /// <summary>
        /// When overridden in a derived class, executes the task.
        /// </summary>
        /// <returns>true if the task successfully executed; otherwise, false.</returns>
        public override bool Execute() {
            return JsonToCsMsBuildTask.ExecuteTask(InputFiles, OutputFiles, Log);
        }
    }

    /// <summary>
    /// Task that reads the versionnumber of the csharp codegenerator and returns it as
    /// output.
    /// </summary>
    public class GetTypedJsonCSharpCodegenVersionTaskNoLocking : AppDomainIsolatedTask {
        /// <summary>
        /// 
        /// </summary>
        [Output]
        public long CSharpCodegenVersion { get; set; }

        /// <summary>
        /// When overridden in a derived class, executes the task.
        /// </summary>
        /// <returns>true if the task successfully executed; otherwise, false.</returns>
        public override bool Execute() {
            CSharpCodegenVersion = SXP.PartialClassGenerator.CSHARP_CODEGEN_VERSION;
            return true;
        }
    }
}
