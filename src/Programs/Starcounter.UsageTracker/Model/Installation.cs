using System;
using System.Collections;
using Starcounter;
using Starcounter.Applications.UsageTrackerApp.Model;
using Starcounter.Applications.UsageTrackerApp.API.Starcounter;
using System.Collections.Generic;

namespace Starcounter.Applications.UsageTrackerApp.Model {

    /// <summary>
    /// Installation
    /// One installation per Installer start /the enduser clicked the installer exe(.
    /// </summary>
    [Database]
    public class Installation {

        /// <summary>
        /// Serial ID (Unique id that identifies a specific build)
        /// </summary>
        public string Serial;

        /// <summary>
        /// Previous Installation number
        /// -1 if there was no previous detected installation
        /// </summary>
        public Int64 PreviousInstallationNo;

        /// <summary>
        /// Installation Number
        /// One number per installation start
        /// </summary>
        public Int64 InstallationNo;

        /// <summary>
        /// Date when the installation was detected
        /// </summary>
        public DateTime Date;

        /// <summary>
        /// Starcounter usages
        /// </summary>
        public IEnumerable Usages {
            get {
                return Db.SlowSQL("SELECT o FROM StarcounterUsage o WHERE o.Installation=?", this);
            }
        }

        /// <summary>
        /// Installer starts
        /// when the user executed the installer executable.
        /// </summary>
        public InstallerStart InstallerStart {
            get {
                return Db.SlowSQL<InstallerStart>("SELECT o FROM InstallerStart o WHERE o.Installation=?", this).First;
            }
        }

        /// <summary>
        /// Installer Executing
        /// when the installer is executing, installing/Uninstalling some software
        /// </summary>
        public InstallerExecuting InstallerExecuting {
            get {
                return Db.SlowSQL<InstallerExecuting>("SELECT o FROM InstallerExecuting o WHERE o.Installation=?", this).First;
            }
        }

        /// <summary>
        /// Installer Aborts
        /// user aborted the installer
        /// </summary>
        public InstallerAbort InstallerAbort {
            get {
                return Db.SlowSQL<InstallerAbort>("SELECT o FROM InstallerAbort o WHERE o.Installation=?", this).First;
            }
        }

        /// <summary>
        /// Installer Finish
        /// finished installation executes
        /// </summary>
        public InstallerFinish InstallerFinish {
            get {
                return Db.SlowSQL<InstallerFinish>("SELECT o FROM InstallerFinish o WHERE o.Installation=?", this).First;
            }
        }

        /// <summary>
        /// Installer End
        ///  when the installer end's (Process ends)
        /// </summary>
        public IEnumerable InstallerEnd {
            get {
                return Db.SlowSQL("SELECT o FROM InstallerEnd o WHERE o.Installation=?", this);
            }
        }

        /// <summary>
        /// Starcounter Usage
        /// </summary>
        public IEnumerable StarcounterGeneral {
            get {
                return Db.SlowSQL("SELECT o FROM StarcounterGeneral o WHERE o.Installation=?", this);
            }
        }


        /// <summary>
        /// Get Next installation
        /// </summary>
        /// <returns></returns>
        public static Installation GetNextNode(Installation installation) {

            Installation currentInstallation = Db.SlowSQL<Installation>("SELECT o FROM Installation o WHERE o.PreviousInstallationNo=?", installation.InstallationNo).First;
            if (currentInstallation == null) return null;

            if (currentInstallation.Serial == "000000000000000000000000") {
                return Installation.GetNextNode(currentInstallation);
            }

            return currentInstallation;

            //return Db.SlowSQL<Installation>("SELECT o FROM Installation o WHERE o.PreviousInstallationNo=?", this.InstallationNo).First;
        }

        /// <summary>
        /// Get Previous installation
        /// </summary>
        /// <returns>Installation</returns>
        public static Installation GetPreviousNode(Installation installation) {

            Installation currentInstallation = Db.SlowSQL<Installation>("SELECT o FROM Installation o WHERE o.InstallationNo=?", installation.PreviousInstallationNo).First;
            if (currentInstallation == null) return null;

            if (installation.PreviousInstallationNo == installation.InstallationNo) {

            }

            if (currentInstallation.Serial == "000000000000000000000000") {
                return Installation.GetPreviousNode(currentInstallation);
            }

            return currentInstallation;

            //return Db.SlowSQL<Installation>("SELECT o FROM Installation o WHERE o.InstallationNo=?", this.PreviousInstallationNo).First;
        }

        /// <summary>
        /// Get First Installation
        /// </summary>
        /// <param name="installation"></param>
        /// <returns>Installation</returns>
        public static Installation GetFirstInstallationNode(Installation installation) {

            Installation currentInstallation = Installation.GetFirstNode(installation);

            while (currentInstallation != null) {

                if (currentInstallation.InstallerFinish != null && currentInstallation.InstallerFinish.Success == true) {

                    if (currentInstallation.InstallerFinish.Mode == 1) { // Installation
                        return currentInstallation;
                    }
                }

                currentInstallation = Installation.GetNextNode(currentInstallation);

            }

            return null;
        }

        /// <summary>
        /// Get the first installation in an installation chain
        /// </summary>
        /// <param name="installation">Installation</param>
        /// <returns>Installation</returns>
        public static Installation GetFirstNode(Installation installation) {

            Installation firstInstallation = null;

            while (true) {
                Installation previousIntallation = Installation.GetPreviousNode(installation);

                // TODO: Prevent infinity loop

                if (previousIntallation == null) {
                    firstInstallation = installation;
                    break;
                }
                installation = previousIntallation;
            }

            return firstInstallation;
        }


        /// <summary>
        /// Get the first installation in an installation chain
        /// </summary>
        /// <param name="installation">Installation</param>
        /// <returns>Installation</returns>
        public static Installation GetLastNode(Installation installation) {

            Installation lastInstallation = null;

            while (true) {
                Installation nextInstallation = Installation.GetNextNode(installation);

                // TODO: Prevent infinity loop
                if (nextInstallation == null) {
                    lastInstallation = installation;
                    break;
                }
                installation = nextInstallation;
            }

            return lastInstallation;
        }

           
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<Installation> GetAllFirstNodes() {

            List<Installation> machines = new List<Installation>();
            //var result = Db.SlowSQL<Installation>("SELECT o FROM Installation o WHERE \"Date\" >= ? AND \"Date\" < ?", from, to);
            var result = Db.SlowSQL<Installation>("SELECT o FROM Installation o");

            foreach (Installation installation in result) {

                if (installation.Serial == "000000000000000000000000") {
                    continue;
                }

                var aInstallation = Installation.GetPreviousNode(installation);
                if (aInstallation == null) {
                    // First time installation
                    machines.Add(installation);
                }
            }
            return machines;

        }


        /// <summary>
        /// Checks if installation was installed in the datetime range
        /// </summary>
        /// <remarks>A return value of false dosent mean that the installation is not installed</remarks>
        /// <param name="installation"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns>The installation when it was installed othterwise null</returns>
        public static Installation GetWhenInstallationWasInstalled(Installation installation, DateTime from, DateTime to) {

            Installation currentInstallation = Installation.GetLastNode(installation);

            while (currentInstallation != null) {

                if (currentInstallation.InstallerFinish != null && currentInstallation.InstallerFinish.Success == true) {

                    // Within date range
                    if (currentInstallation.Date >= from && currentInstallation.Date < to) {

                        if (currentInstallation.InstallerFinish.Mode == 1) { // Installation
                            return currentInstallation;
                        }
                        else if (currentInstallation.InstallerFinish.Mode == 3) { // UnInstallation
                            return null;
                        }
                    }
                }

                currentInstallation = Installation.GetPreviousNode(currentInstallation);
            }

            return null;
        }


        /// <summary>
        /// Checks if installation was uninstalled in the datetime range
        /// </summary>
        /// <remarks>A return value of false dosent mean that the installation is not uninstalled</remarks>
        /// <param name="installation"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns>The uninstallation when it was installed othterwise null</returns>
        public static Installation GetWhenInstallationWasUninstalled(Installation installation, DateTime from, DateTime to) {

            Installation currentInstallation = Installation.GetLastNode(installation);

            while (currentInstallation != null) {

                if (currentInstallation.InstallerFinish != null && currentInstallation.InstallerFinish.Success == true) {

                    // Within date range
                    if (currentInstallation.Date >= from && currentInstallation.Date < to) {

                        if (currentInstallation.InstallerFinish.Mode == 3) { // UnInstallation
                            return currentInstallation;
                        }
                        else if (currentInstallation.InstallerFinish.Mode == 1) { // Installation
                            return null;
                        }
                    }
                }

                currentInstallation = Installation.GetPreviousNode(currentInstallation);
            }

            return null;
        }

    }


}
