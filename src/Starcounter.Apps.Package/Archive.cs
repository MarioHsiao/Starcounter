using Microsoft.Build.Evaluation;
using Starcounter.Apps.Package.Config;
using Starcounter.Internal;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace Starcounter.Apps.Package {
    public partial class Archive {

        const string ArchiveResourceFolder = "wwwroot";    // Zip archive resource folder
        const string ArchiveBinaryFolder = "app";          // Zip archive binary folder

        #region Properties
        /// <summary>
        /// Fullpath to resource folder (i.e wwwroot)
        /// </summary>
        public string ResourceFolder { get; private set; }
        /// <summary>
        /// Fullpath to executable file
        /// </summary>
        public string Executable { get; private set; }
        /// <summary>
        /// Fullpath to binaryfolder,
        /// </summary>
        public string BinaryFolder { get; private set; }
        /// <summary>
        /// Fullpath to icon image,
        /// </summary>
        public string Icon { get; private set; }
        /// <summary>
        /// Fullpath to Zip archive
        /// </summary>
        public string FileName { get; private set; }
        /// <summary>
        /// Fullpath to project file
        /// </summary>
        public string ProjectFile { get; private set; }

        /// <summary>
        /// Package configuration
        /// </summary>
        public IConfig Config { get; private set; }

        #endregion

        /// <summary>
        /// Create archive
        /// Uses Visual studio project file found in current folder
        /// </summary>
        /// <returns>Archive</returns>
        public static Archive Create() {

            // Find csproj in current directory
            return Create(null);
        }

        /// <summary>
        /// Create archive
        /// </summary>
        /// <param name="file">Visual studio project file.</param>
        /// <returns>Archive</returns>
        public static Archive Create(string file) {
            return Create(file, null, null);
        }

        /// <summary>
        /// Create Archive from visual studio project file.
        /// </summary>
        /// <param name="file">Visual studio project file.</param>
        /// <param name="executable">Path to Executable</param>
        /// <param name="resourceFolder">Path to Resource folder</param>
        /// <returns>Archive</returns>
        public static Archive Create(string file = null, string executable = null, string resourceFolder = null) {

            if (string.IsNullOrEmpty(file)) {

                string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj");

                if (files.Length == 0) {
                    throw new InputErrorException(string.Format("Visual Studio project file (.csproj) not found in current folder ({0})", Directory.GetCurrentDirectory()));
                }

                if (files.Length > 1) {
                    throw new InputErrorException(string.Format("Multiple Visual Studio project files found in current folder ({0})", Directory.GetCurrentDirectory()));
                }

                file = files[0];
            }

            if (!File.Exists(file)) {
                throw new InputErrorException(string.Format("Visual Studio project file not found ({0})", file));
            }

            Project project = ProjectCollection.GlobalProjectCollection.LoadProject(file);
            Archive archive = new Archive();
            archive.ProjectFile = project.FullPath; ;
            archive.ResourceFolder = archive.GetResourceFolder(resourceFolder, project);
            archive.Executable = archive.GetExecutable(executable, project);
            archive.BinaryFolder = Path.GetDirectoryName(archive.Executable);
            if (!string.IsNullOrEmpty(archive.BinaryFolder)) {
                archive.BinaryFolder = Utils.PathAddBackslash(archive.BinaryFolder);
            }
            archive.Icon = archive.GetIcon(project);

            archive.Config = archive.CreatePackageConfig();

            return archive;
        }

        /// <summary>
        /// Retrive resource folder
        /// </summary>
        /// <param name="resourceFolder"></param>
        /// <param name="fallback"></param>
        /// <returns>Full path to resource folder</returns>
        private string GetResourceFolder(string resourceFolder = null, Project fallback = null) {

            if (string.IsNullOrEmpty(resourceFolder)) {

                string rootPath = null;

                if (fallback != null) {

                    if (string.IsNullOrEmpty(fallback.DirectoryPath)) {
                        throw new InputErrorException("Missing directory path in project file");
                    }

                    rootPath = fallback.DirectoryPath;

                    // TODO: Try to retrive resource folder from project working folder.
                    //ProjectProperty workingDirectoryProperty = p.GetProperty("StartWorkingDirectory");
                    //if (workingDirectoryProperty != null) {
                    //    resourceFolder = workingDirectoryProperty.EvaluatedValue;
                    //}
                    //else {
                    //    resourceFolder = null;
                    //}

                }
                else {
                    rootPath = Directory.GetCurrentDirectory();
                }

                string wwwrootFolder = System.IO.Path.Combine(rootPath, ArchiveResourceFolder);

                if (!string.IsNullOrEmpty(wwwrootFolder)) {
                    wwwrootFolder = Utils.PathAddBackslash(wwwrootFolder);
                }

                if (!System.IO.Directory.Exists(wwwrootFolder)) {
                    return null;
                    //    throw new InvalidOperationException(string.Format("Missing wwwroot folder in project folder {0}.", rootPath));
                }
                return wwwrootFolder;
            }


            if (!string.IsNullOrEmpty(resourceFolder)) {
                resourceFolder = Utils.PathAddBackslash(resourceFolder);
            }

            if (!System.IO.Directory.Exists(resourceFolder)) {
                throw new InputErrorException(string.Format("Resource folder not found ({0})", resourceFolder));
            }

            return resourceFolder;
        }

        /// <summary>
        /// Retrive executable 
        /// </summary>
        /// <param name="executable"></param>
        /// <param name="fallback"></param>
        /// <returns>Fullpath to executable</returns>
        private string GetExecutable(string executable = null, Project fallback = null) {

            if (string.IsNullOrEmpty(executable)) {

                if (fallback != null) {
                    string outputPath = fallback.GetPropertyValue("OutputPath");
                    if (string.IsNullOrEmpty(outputPath)) {
                        throw new InputErrorException(string.Format("Failed to find executable from the output path in the project file ({0})", fallback.FullPath));
                    }

                    if (!System.IO.Path.IsPathRooted(outputPath)) {
                        // Make it absolut
                        outputPath = System.IO.Path.Combine(fallback.DirectoryPath, outputPath);
                    }

                    string outputType = fallback.GetPropertyValue("OutputType");
                    if (!string.Equals("exe", outputType, StringComparison.InvariantCultureIgnoreCase)) {
                        throw new InputErrorException(string.Format("Invalid project type ({0})", outputType));
                    }

                    string assemblyName = fallback.GetPropertyValue("AssemblyName");
                    if (string.IsNullOrEmpty(assemblyName)) {
                        throw new InputErrorException(string.Format("Invalid or empty project Assembly name in project file ({0})", fallback.FullPath));
                    }

                    executable = Path.Combine(outputPath, assemblyName + "." + outputType.ToLower());
                }
                else {

                    // check bin/release and bin/debug for executable
                    string rootPath = Directory.GetCurrentDirectory();
                    string outputPath = null;
                    string releasePath = Path.Combine(rootPath, "bin/release");
                    string debugPath = Path.Combine(rootPath, "bin/debug");
                    if (!Directory.Exists(releasePath)) {
                        if (!Directory.Exists(debugPath)) {
                            throw new InputErrorException(string.Format("Executable not found in ({0}) or ({1})", releasePath, debugPath));
                        }
                        outputPath = debugPath;
                    }
                    else {
                        outputPath = releasePath;
                    }

                    if (string.IsNullOrEmpty(outputPath)) {
                        throw new InputErrorException(string.Format("Executable not found"));
                    }

                    // Find Executable (first .exe file )
                    //                    string[] executables = System.IO.Directory.GetFiles(outputPath, "*.exe");

                    DirectoryInfo info = new DirectoryInfo(outputPath);
                    FileInfo[] executables = info.GetFiles("*.exe").OrderByDescending(p => p.LastWriteTime).ToArray();

                    if (executables.Length > 0) {
                        executable = executables.First<FileInfo>().FullName;
                    }
                }
            }

            if (!File.Exists(executable)) {
                throw new InputErrorException(string.Format("Executable not found ({0})", executable));
            }

            return executable;
        }

        /// <summary>
        /// Get icon
        /// </summary>
        /// <param name="project"></param>
        /// <returns>Fullpath to icon</returns>
        private string GetIcon(Project project) {

            if (project == null) throw new ArgumentNullException("Project");

            if (string.IsNullOrEmpty(project.DirectoryPath)) {
                throw new InputErrorException("Directory path not found project file");
            }

            string[] exts = new string[] { ".svg", ".png", ".jpg", ".jpeg", ".gif", ".bmp" };

            string projectName = Path.GetFileNameWithoutExtension(project.FullPath);

            string[] files = Directory.GetFiles(project.DirectoryPath, projectName + ".*");

            foreach (string file in files) {
                string fileExt = Path.GetExtension(file).ToLower();
                foreach (string ext in exts) {
                    if (ext == fileExt) {
                        return file;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Save archive to zip file
        /// </summary>
        /// <param name="file">Path to output file</param>
        public void Save(string file) {

            Save(file, false);
        }

        /// <summary>
        /// Save archive to zip file
        /// </summary>
        /// <param name="file">Path to output file</param>
        /// <param name="overwrite">true if existing file will be overwritten</param>
        public void Save(string file, bool overwrite) {

            if (string.IsNullOrEmpty(file)) throw new ArgumentNullException("File");
            if (this.Config == null) throw new ArgumentNullException("Config");

            if (overwrite == false && File.Exists(file)) {
                throw new InputErrorException(string.Format("Output file exists ({0})", file));
            }

            if (!Directory.Exists(Path.GetDirectoryName(file))) {
                throw new InputErrorException(string.Format("Output folder not found ({0})", Path.GetDirectoryName(file)));
            }

            this.VerifyConfiguration();

            MemoryStream memoryStream = new MemoryStream();

            using (ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true)) {

                if (!string.IsNullOrEmpty(this.ResourceFolder)) {
                    archive.CreateEntry(ArchiveResourceFolder + "/"); // TODO: Workaround so Warehouse / Starcounter Admin VerifyPackageEntry will work.
                    Archive.AddFolderToZipArchive(archive, this.ResourceFolder, ArchiveResourceFolder);
                }
                Archive.AddFolderToZipArchive(archive, this.BinaryFolder, ArchiveBinaryFolder);

                if (!string.IsNullOrEmpty(this.Icon) && File.Exists(this.Icon)) {
                    archive.CreateEntryFromFile(this.Icon, Path.GetFileName(this.Icon));
                }

                ZipArchiveEntry entry = archive.CreateEntry("package.config");
                using (StreamWriter writer = new StreamWriter(entry.Open())) {
                    writer.Write(this.Config.GetString());
                }
            }

            using (FileStream fileStream = new FileStream(file, FileMode.Create)) {
                memoryStream.Seek(0, SeekOrigin.Begin);
                memoryStream.CopyTo(fileStream);
            }

            this.FileName = file;
        }

        /// <summary>
        /// Create package configuration
        /// </summary>
        /// <returns>Configuration</returns>
        private IConfig CreatePackageConfig() {

            PackageConfigFile config = new PackageConfigFile();
            config.Channel = null;
            config.Heading = string.Empty; // TODO
            config.ImageUri = (this.Icon != null) ? Path.GetFileName(this.Icon) : null;
            config.Executable = Path.Combine(ArchiveBinaryFolder, Path.GetFileName(this.Executable));
            config.ResourceFolder = ArchiveResourceFolder;
            config.AppName = Path.GetFileNameWithoutExtension(this.Executable);
            config.VersionDate = DateTime.MinValue;

            string starcounterVersion = Archive.GetStarcounterDependency();
            if (!string.IsNullOrEmpty(starcounterVersion)) {
                config.Dependencies = new[] { new Dependency() { Name = "Starcounter", Value = "~" + starcounterVersion } };
            }
            string version;
            string channel;
            this.ReadAssemblyFile(this.Executable, out config.Namespace, out config.DisplayName, out version, out config.Company, out config.Description, out config.VersionDate, out channel);
            config.Version = version;
            config.Channel = channel;

            return config;
        }

        /// <summary>
        /// Add existing folder to zip archive
        /// </summary>
        /// <param name="archive"></param>
        /// <param name="folderToInclude"></param>
        /// <param name="archiveFolderName"></param>
        private static void AddFolderToZipArchive(ZipArchive archive, string folderToInclude, string archiveFolderName) {

            foreach (string file in Directory.EnumerateFileSystemEntries(folderToInclude, "*", SearchOption.AllDirectories)) {
                string newFile = Path.Combine(archiveFolderName, file.Substring(folderToInclude.Length));

                FileAttributes attr = File.GetAttributes(file);
                if (attr.HasFlag(FileAttributes.Directory)) {
                    // Its a directory
                    archive.CreateEntry(newFile + Path.AltDirectorySeparatorChar);
                }
                else {
                    // Its a file"
                    archive.CreateEntryFromFile(file, newFile);
                }
            }
        }

        /// <summary>
        /// Read assmebly
        /// </summary>
        /// <param name="file"></param>
        /// <param name="id"></param>
        /// <param name="title"></param>
        /// <param name="version"></param>
        /// <param name="company"></param>
        /// <param name="description"></param>
        /// <param name="buildDate"></param>
        /// <param name="channel"></param>
        private void ReadAssemblyFile(string file, out string id, out string title, out string version, out string company, out string description, out DateTime buildDate, out string channel) {

            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(file);

            if (!string.IsNullOrEmpty(fvi.ProductVersion)) {
                version = fvi.ProductVersion;
            }
            else {
                version = "1.0.0.0";
            }
            id = null;
            channel = null;
            title = fvi.ProductName;
            company = fvi.CompanyName;
            description = fvi.Comments;
            buildDate = File.GetLastWriteTimeUtc(file);
            System.Reflection.Assembly assembly = Assembly.LoadFile(file);

            string ns = assembly.EntryPoint.DeclaringType.Namespace;

            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute), false);

            if (attributes.Length > 0) {

                foreach (AssemblyMetadataAttribute item in attributes) {
                    if (item.Key.ToUpper() == "ID") {
                        id = item.Value;
                    }
                    else if (item.Key.ToUpper() == "CHANNEL") {
                        channel = item.Value;
                    }
                }

            }

            if (id == null) {
                id = ns + "." + assembly.GetName().Name;
            }

            if (channel == null) {
                channel = "Stable";
            }
        }

        /// <summary>
        /// Get starcounter version
        /// </summary>
        /// <returns></returns>
        private static string GetStarcounterDependency() {

            string starcounterBin;

            try {
                starcounterBin = StarcounterEnvironment.InstallationDirectory;
            }
            catch (Exception) {
                // Starcounter not installed
                return null;
            }

            string versionFile = Path.Combine(starcounterBin, StarcounterEnvironment.FileNames.VersionInfoFileName);
            if (!File.Exists(versionFile)) {
                // Invalid starcounter installation
                return null;
            }

            string[] lines = File.ReadAllLines(versionFile);

            foreach (string line in lines) {

                // <Version>2.2.1.2385</Version>"
                int p1 = line.IndexOf("<Version>");
                if (p1 != -1) {
                    int p2 = line.IndexOf("</Version>");
                    if (p2 != -1) {
                        return line.Substring(p1 + 9, p2 - (p1 + 9));
                    }
                }
            }
            return null;
        }

        #region Verify

        private void VerifyConfiguration() {

            // TODO:
        }

        #endregion

    }
}
