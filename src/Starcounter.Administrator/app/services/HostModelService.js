/**
 * ----------------------------------------------------------------------------
 * Host model Service
 * Refreshes the databases model and the applications model
 * ----------------------------------------------------------------------------
 */
adminModule.service('HostModelService', ['$http', '$log', 'UtilsFactory', 'DatabaseService', 'ApplicationService', 'InstalledApplicationService', function ($http, $log, UtilsFactory, DatabaseService, ApplicationService, InstalledApplicationService) {

    // List of databases
    // {
    //     "Name":"tracker",
    //     "Uri":"http://machine:1234/api/databases/mydatabase",
    //     "HostUri":"http://machine:1234/api/engines/mydatabase/db",
    //     "Running":true,
    //     "console":"",
    //      consoleManualMode : false
    // }
    this.databases = DatabaseService.databases;


    // List of applications
    //  {
    //      "Uri": "http://example.com/api/executables/foo/foo.exe-123456789",
    //      "Path": "C:\\path\to\\the\\exe\\foo.exe",
    //      "ApplicationFilePath" : "C:\\optional\\path\to\\the\\input\\file.cs",
    //      "Name" : "Name of the application",
    //      "Description": "Implements the Foo module",
    //      "Arguments": [{"dummy":""}],
    //      "DefaultUserPort": 1,
    //      "ResourceDirectories": [{"dummy":""}],
    //      "WorkingDirectory": "C:\\path\\to\\default\\resource\\directory",
    //      "IsTool":false,
    //      "StartedBy": "Per Samuelsson, per@starcounter.com",
    //      "Engine": {
    //          "Uri": "http://example.com/api/executables/foo"
    //      },
    //      "RuntimeInfo": {
    //         "LoadPath": "\\relative\\path\\to\\weaved\\server\\foo.exe",
    //         "Started": "ISO-8601, e.g. 2013-04-25T06.24:32",
    //         "LastRestart": "ISO-8601, e.g. 2013-04-25T06.49:01"
    //      },
    //      databaseName : "default",
    //      key : "foo.exe-123456789",
    //      console : "console output",
    //      consoleManualMode : false
    //  }
    this.applications = ApplicationService.applications;


    // Installed applications
    this.installedApplications = InstalledApplicationService.installedApplications;

    /**
     * Get Application
     * @param {string} databaseName Database name
     * @param {string} applicationName Application name
     * @return {object} Application or null
     */
    this.getApplication = function (databaseName, applicationName) {
        return ApplicationService.getApplication(databaseName, applicationName);
    }


    /**
     * Get database
     * @param {string} databaseName Database name
     * @return {object} Database or null
     */
    this.getDatabase = function (databaseName) {
        return DatabaseService.getDatabase(databaseName);
    }


    /**
     * Refresh host model (Databases and Applications)
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.refreshHostModel = function (successCallback, errorCallback) {

        DatabaseService.refreshDatabases(function () {

            // Success
            ApplicationService.refreshApplications(function () {

                // Success
                if (typeof (successCallback) == "function") {
                    successCallback();
                }

            }, function (messageObject) {

                // Error
                if (typeof (errorCallback) == "function") {
                    errorCallback(messageObject);
                }

            });

        }, function (messageObject) {

            // Error
            if (typeof (errorCallback) == "function") {
                errorCallback(messageObject);
            }
        });


        InstalledApplicationService.refreshInstalledApplications(function () {

            // Success
            if (typeof (successCallback) == "function") {
                successCallback();
            }

        }, function (messageObject) {

            // Error
            if (typeof (errorCallback) == "function") {
                errorCallback(messageObject);
            }

        });
    }

}]);