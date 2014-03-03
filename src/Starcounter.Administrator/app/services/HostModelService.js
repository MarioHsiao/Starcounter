/**
 * ----------------------------------------------------------------------------
 * Host model Service
 * Refreshes the databases model and executables model
 * ----------------------------------------------------------------------------
 */
adminModule.service('HostModelService', ['$http', '$log', 'UtilsFactory', 'DatabaseService', 'ExecutableService', function ($http, $log, UtilsFactory, DatabaseService, ExecutableService) {

    // List of databases
    // {
    //     "Name":"tracker",
    //     "Uri":"http://machine:1234/api/databases/mydatabase",
    //     "HostUri":"http://machine:1234/api/engines/mydatabase/db",
    //     "Running":true,
    //     "console":""
    // }
    this.databases = DatabaseService.databases;


    // List of executables
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
    this.executables = ExecutableService.executables;


    /**
     * Get executable
     * @param {executableName} executableName Executable name
     * @return {executable} Executable or null
     */
    this.getExecutable = function (executableName) {
        return ExecutableService.getExecutable(executableName);
    }


    /**
     * Get database
     * @param {databaseName} databaseName Database name
     * @return {database} Database or null
     */
    this.getDatabase = function (databaseName) {
        return DatabaseService.getDatabase(databaseName);
    }


    /**
     * Refresh host model (Databases and Executables)
     * @param {successCallback} successCallback function
     * @param {errorCallback} errorCallback function
     */
    this.refreshHostModel = function (successCallback, errorCallback) {

        DatabaseService.refreshDatabases(function () {
            // Success

            ExecutableService.refreshExecutables(function () {

                // Success
                if (typeof (successCallback) == "function") {
                    successCallback();
                }

            }, function (messageObject) {
                // Error
                UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
            });


        }, function (messageObject) {
            // Error
            UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
        });
     
    }

}]);