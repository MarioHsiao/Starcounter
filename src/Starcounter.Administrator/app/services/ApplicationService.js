/**
 * ----------------------------------------------------------------------------
 * Applications Service
 * ----------------------------------------------------------------------------
 */
adminModule.service('ApplicationService', ['$http', '$log', '$sce', 'ConsoleService', 'UtilsFactory', 'JobFactory', function ($http, $log, $sce, ConsoleService, UtilsFactory, JobFactory) {

    var self = this;

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
    //
    //      databaseName : "default",
    //      key : "D1730AE8464E90FE192C8B22AEE3F1C1E41A61BD",
    //      console : "console output",
    //      consoleManualMode : false,
    //      running : true
    //  }
    this.applications = [];


    /**
     * Get all running applications
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.getApplications = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrieve a list of applications";
        var uri = "/api/admin/applications";

        // Example JSON response 
        //-----------------------
        //{
        //    "Executables":[{
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
        //      }
        //    }]
        //}
        $http.get(uri).then(function (response) {
            // Success

            $log.info("Applications (" + response.data.Items.length + ") successfully retrived");
            if (typeof (successCallback) == "function") {

                // Add some extra properties
                for (var i = 0; i < response.data.Items.length ; i++) {
                    response.data.Items[i].databaseName = response.data.Items[i].Engine.Uri.replace(/^.*[\\\/]/, '');
                    response.data.Items[i].running = true;
                    response.data.Items[i].key = response.data.Items[i].Uri.replace(/^.*[\\\/]/, '')
                    //response.data.Items[i].consoleManualMode = false;
                }

                successCallback(response.data.Items);
            }

        }, function (response) {
            // Error

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {
                var messageObject;

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 500) {
                    // 500 Server Error
                    errorHeader = "Internal Server Error";
                    messageObject = UtilsFactory.createServerErrorMessage(errorHeader, response.data);
                }
                else {
                    // Unhandle Error
                    messageObject = UtilsFactory.createServerErrorMessage(errorHeader, response.data);
                }

                errorCallback(messageObject);
            }

        });
    }


    /**
     * Get Application
     * @param {string} databaseName Database name
     * @param {string} applicationName Application name
     * @return {object} Application or null
     */
    this.getApplication = function (databaseName, applicationName) {

        for (var i = 0 ; i < self.applications.length ; i++) {
            if (self.applications[i].databaseName == databaseName && self.applications[i].Name == applicationName) {
                return self.applications[i];
            }
        }
        return null;
    }


    /**
     * Pick an applications by opening a filedialog on the server
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.pickApplications = function (successCallback, errorCallback) {

        var errorHeader = "Failed to pick an application";
        var uri = "/api/admin/openappdialog";

        // Example JSON response 
        //-----------------------
        //    [{
        //      "file" : "fullpathtofile"
        //    }]
        $http.get(uri).then(function (response) {
            // Success

            $log.info("Picked Applications (" + response.data.length + ") successfully retrived");
            if (typeof (successCallback) == "function") {
                successCallback(response.data);
            }

        }, function (response) {

            if (response.status == 404) {
                // No files selected
                successCallback(response.data);
                return;
            }

            // Error
            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {
                var messageObject;

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 403) {
                    // 500 Server Error
                    errorHeader = "Browse files is only allowed when the browser and the server is on the same machine";
                    messageObject = UtilsFactory.createServerErrorMessage(errorHeader, response.data);
                }
                else if (response.status == 500) {
                    // 500 Server Error
                    errorHeader = "Internal Server Error";
                    messageObject = UtilsFactory.createServerErrorMessage(errorHeader, response.data);
                }
                else {
                    // Unhandle Error
                    messageObject = UtilsFactory.createServerErrorMessage(errorHeader, response.data);
                }

                errorCallback(messageObject);
            }

        });
    }

    /**
     * Refresh applications
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.refreshApplications = function (successCallback, errorCallback) {

        this.getApplications(function (applications) {
            // Success

            // Update the current applications list with the new applications list
            self._updateApplicationsList(applications);

            if (typeof (successCallback) == "function") {
                successCallback();
            }

        }, function (response) {
            // Error

            if (typeof (errorCallback) == "function") {
                errorCallback(response);
            }

        });

    }


    /**
     * Refresh application console output
     * @param {object} application Application
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.refreshConsoleOuput = function (application, successCallback, errorCallback) {

        ConsoleService.getConsoleOuput(application.databaseName, { databaseName: application.databaseName, applicationName: application.Name }, function (consoleEvents) {
            // Success

            var consoleText = "";
            for (var i = 0; i < consoleEvents.length; i++) {
                consoleText = consoleText + consoleEvents[i].text;
            }
            self._onConsoleOutputEvent(application, consoleText, false);

            if (typeof (successCallback) == "function") {
                successCallback();
            }

        }, errorCallback);

    }


    /**
     * Callback when there is an incoming console message
     * @param {object} application Application
     * @param {text} text Text to console
     * @param {boolean} bAppend True is Text will be appended to current console text
     */
    this._onConsoleOutputEvent = function (application, text, bAppend) {

        var result;

        if (bAppend) {
            result = application.console + text;
        }
        else {
            result = text;
        }

        // Limit the buffer
        if (result.length > self.bufferSize) {
            result = result.substr(result.length - self.bufferSize);
        }

        application.console = result;
    }


    /**
     * Merge New Applications with history applications
     * @return {array} Application list
     */
    this._mergeNewListWithHistoryList = function (freshApplications, historyApplications) {


        // Added history applications if they dosent exist is newApplicationlist
        for (var i = 0; i < historyApplications.length ; i++) {

            var historyApplication = historyApplications[i];

            var bExists = false;

            for (var x = 0; x < freshApplications.length; x++) {
                var newApplication = freshApplications[x];

                if (historyApplication.databaseName == newApplication.databaseName && historyApplication.Name == newApplication.Name) {
                    bExists = true;
                    break;
                }
            }

            if (bExists == false) {
                // The history applicaion is not running, update it's running property
                historyApplications[i].console = null;
                historyApplications[i].running = false;
                freshApplications.push(historyApplications[i]);
            }

        }

        return freshApplications;
    }


    /**
     * Update current application list with new list
     * @param {array} newDatabases New application list
     */
    this._updateApplicationsList = function (freshApplications) {

        //var newList = [];
        var removeList = [];

        var historyApplications = this._getHistoryApplications();

        // Combination of running and history applications
        var freshApplications = this._mergeNewListWithHistoryList(freshApplications, historyApplications);


        // Check for new application and update current applications
        for (var i = 0; i < freshApplications.length; i++) {
            var freshApplication = freshApplications[i];

            var application = this.getApplication(freshApplication.databaseName, freshApplication.Name);

            if (application == null) {
                // freshApplication can be a new started applications or a history application

                self.applications.push(freshApplication);

                if (freshApplication.running == true) {
                    // TODO: Check this
                    freshApplication.console = "";
                    freshApplication.consoleManualMode = false;

                    self._onApplicationStarted(freshApplication);
                }

            } else {

                UtilsFactory.updateObject(freshApplication, application, function (arg) {

                    if (arg.propertyName == "running") {

                        if (arg.newValue) {
                            self._onApplicationStarted(arg.source);
                        }
                        else {
                            self._onApplicationStopped(arg.source);
                        }
                    }

                });
            }
        }


        // Remove removed applications from application list
        for (var i = 0; i < self.applications.length; i++) {

            var application = self.applications[i];
            var bExists = false;
            // Check if it exist in newList
            for (var x = 0; x < freshApplications.length; x++) {
                var freshApplication = freshApplications[x];

                if (application.databaseName == freshApplication.databaseName && application.Name == freshApplication.Name) {
                    bExists = true;
                    break;
                }
            }

            if (bExists == false) {
                removeList.push(application);
            }
        }


        // Remove application from application list
        for (var i = 0; i < removeList.length; i++) {
            var index = self.applications.indexOf(removeList[i]);
            if (index > -1) {
                self.applications.splice(index, 1);
            }
            removeList[i].running = false;
            this._onApplicationStopped(removeList[i]);

        }

        // save applications history
        this._SaveHistory(self.applications);

    }


    /**
     * On application started event
     * @param {object} application Database
     */
    this._onApplicationStarted = function (application) {

        $log.info("Application started : " + application.Name + "(" + application.databaseName + ")");

        application.running = true;
        application.console = "";

        // Socket event listener
        application.consoleListener = {
            databaseName: application.databaseName,
            onEvent: function (consoleEvents) {

                for (var i = 0; i < consoleEvents.length; i++) {
                    self._onConsoleOutputEvent(application, consoleEvents[i].text, true);
                }

            },
            onError: function (messageObject) {

                // Sliently fallback to manual mode
                application.consoleManualMode = true;

            },
            filter: { databaseName: application.databaseName, applicationName: application.Name }
        }

        ConsoleService.registerEventListener(application.consoleListener);

    }


    /**
     * On application stopped event
     * @param {object} application Application
     */
    this._onApplicationStopped = function (application) {

        $log.info("Application stopped : " + application.Name + "(" + application.databaseName + ")");

        application.console = "";

        ConsoleService.unregisterEventListener(application.consoleListener);
    }


    /**
     * Get history applications
     * @return {array} Applications
     */
    this._getHistoryApplications = function () {

        // Check for  web storage support..
        if (typeof (Storage) !== "undefined") {

            var result = localStorage.getItem("historyApplications");
            if (result) {
                // TODO: Make is safe $sce.trustAsHtml(application.console);
                return JSON.parse(result);
            }
        }

        return [];
    }


    /**
     * Save applications history
     * @return {array} Applications to be saved in history (this will replace current history)
     */
    this._SaveHistory = function (applications) {

        // Check for  web storage support..
        if (typeof (Storage) === "undefined") {
            return;
        }

        // Save to storage
        localStorage.setItem("historyApplications", JSON.stringify(applications));
    }


    /**
     * Remove application from history
     * @param {object} application Application
     */
    this.removeFromHistory = function (application) {

        var index = self.applications.indexOf(application);
        if (index > -1) {
            self.applications.splice(index, 1);
        }

        // Update history
        this._SaveHistory(self.applications);

    }


    /**
     * Restart Application
     * @param {object} application Application
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.restartApplication = function (application, successCallback, errorCallback) {

        this.stopApplication(application, function () {
            // Success
            self.startApplication(application, successCallback, errorCallback)

        }, function (messageObject) {
            // Error
            if (typeof (errorCallback) == "function") {
                errorCallback(messageObject);
            }
        })
    }


    /**
     * Start Application
     * @param {object} application Application
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.startApplication = function (application, successCallback, errorCallback) {

        this.startEngine(application.databaseName, function () {
            // Success

            var bodyData = {
                "Uri": "",
                "Path": application.Path,
                "ApplicationFilePath": application.ApplicationFilePath,
                "Name": application.Path.replace(/^.*[\\\/]/, ''),
                "Description": "",
                "Arguments": [{
                    "dummy": application.Path
                }],
                "DefaultUserPort": 0,
                "ResourceDirectories": [],
                "WorkingDirectory": application.WorkingDirectory,
                "IsTool": true,
                "StartedBy": application.StartedBy,
                "Engine": { "Uri": "" },
                "RuntimeInfo": {
                    "LoadPath": "",
                    "Started": "",
                    "LastRestart": ""
                }
            };


            // Add job
            var job = { message: "Starting application " + application.Name + " in " + application.databaseName };
            JobFactory.AddJob(job);

            // Example JSON response 
            //-----------------------
            // 201
            // {
            //     "Uri": "http://example.com/api/executables/foo/foo.exe-123456789",
            //     "Path": "C:\\path\to\\the\\exe\\foo.exe",
            //     "ApplicationFilePath" : "C:\\optional\\path\to\\the\\input\\file.cs",
            //     "Description": "Implements the Foo module",
            //     "Arguments": [{"dummy":""}],
            //     "DefaultUserPort": 1,
            //     "ResourceDirectories": [{"dummy":""}],
            //     "WorkingDirectory": "C:\\path\\to\\default\\resource\\directory",
            //     "IsTool":false,
            //     "StartedBy": "Per Samuelsson, per@starcounter.com",
            //     "Engine": {
            //         "Uri": "http://example.com/api/executables/foo"
            //     },
            //     "RuntimeInfo": {
            //         "LoadPath": "\\relative\\path\\to\\weaved\\server\\foo.exe",
            //         "Started": "ISO-8601, e.g. 2013-04-25T06.24:32",
            //         "LastRestart": "ISO-8601, e.g. 2013-04-25T06.49:01"
            //     }
            // }
            $http.post('/api/engines/' + application.databaseName + '/executables', bodyData).then(function (response) {
                // Success
                JobFactory.RemoveJob(job);

                $log.info("Application " + response.data.Path + " was successfully started");

                // Refresh applications
                self.refreshApplications(successCallback, errorCallback);

                // TODO: Return the started application

            }, function (response) {
                // Error
                JobFactory.RemoveJob(job);

                var errorHeader = "Failed to start application";
                $log.error(errorHeader, response);

                if (typeof (errorCallback) == "function") {

                    var messageObject;
                    if (response instanceof SyntaxError) {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                    }
                    else if (response.status == 404) {
                        // 404 A database with the specified name was not found.
                        messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                    }
                    else if (response.status == 409) {
                        // 409 The application is already running or the Engine is not started.
                        messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                    }
                    else if (response.status == 422) {
                        // 422 The application can not be found or The weaver failed to load a binary user code file.
                        messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                    }
                    else if (response.status == 500) {
                        // 500 Server Error
                        errorHeader = "Internal Server Error";
                        messageObject = UtilsFactory.createServerErrorMessage(errorHeader, response.data);
                    }
                    else {
                        // Unhandle Error
                        messageObject = UtilsFactory.createServerErrorMessage(errorHeader, response.data);
                    }

                    errorCallback(messageObject);
                }

            });

        }, function (errorMessageObject) {
            // Error
            if (typeof (errorCallback) == "function") {
                errorCallback(errorMessageObject);
            }
        });

    }


    /**
     * Stop Application
     * @param {object} application Application
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.stopApplication = function (application, successCallback, errorCallback) {

        var job = { message: "Stopping application " + application.Name };
        JobFactory.AddJob(job);

        var uri = UtilsFactory.toRelativePath(application.Uri);

        $http.delete(uri).then(function (response) {
            // Success, 204 No Content 
            JobFactory.RemoveJob(job);

            $log.info("Application " + application.Name + " was successfully stopped");

            // Refresh databases
            self.refreshApplications(successCallback, errorCallback);

        }, function (response) {
            JobFactory.RemoveJob(job);

            // Error
            var errorHeader = "Failed to stop application";
            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                var messageObject;

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 404) {
                    // 404 A database with the specified name was not found.
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                }
                else if (response.status == 409) {
                    // 409 The application is already running or the Engine is not started.
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                }
                else if (response.status == 500) {
                    // 500 Server Error
                    errorHeader = "Internal Server Error";
                    messageObject = UtilsFactory.createServerErrorMessage(errorHeader, response.data);
                }
                else {
                    // Unhandle Error
                    messageObject = UtilsFactory.createServerErrorMessage(errorHeader, response.data);
                }

                errorCallback(messageObject);
            }

        });


    }


    /**
     * Start engine/database
     * @param {string} name Database name
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     * @return {object} promise
     */
    this.startEngine = function (name, successCallback, errorCallback) {

        var errorHeader = "Failed to start database";

        var job = { message: "Starting database " + name };
        JobFactory.AddJob(job);

        var engineData = { Name: name, NoDb: false, LogSteps: false };    // TODO: get NoDb and LogSteps from arguments
        var uri = "/api/engines";

        // Example JSON response (200 Ok)
        //-----------------------
        // {
        //     "CodeHostCommandLineAdditions" : "",
        //     "LogSteps" : false,
        //     "Name" : "default",
        //     "NoDb" : false,
        //     "Uri" : "http://headsutv19:8181/api/engines/tracker"
        // }
        //
        // Example JSON response (201 Created)
        //-----------------------
        // {
        // "Uri":"http://headsutv19:8181/api/engines/tracker",
        // "NoDb":false,
        // "LogSteps":false,
        // "Database":{
        //      "Name":"tracker",
        //      "Uri":"http://headsutv19:8181/api/databases/tracker"
        // },
        // "DatabaseProcess":{
        //      "Uri":"http://headsutv19:8181/api/engines/tracker/db",
        //      "Running":true
        // },
        // "CodeHostProcess":{
        //      "Uri":"http://headsutv19:8181/api/engines/tracker/host",
        //      "PID":11136
        // },
        // "Executables":{
        //      "Uri":"http://headsutv19:8181/api/engines/tracker/executables",
        //      "Executing":[]
        // }
        $http.post(uri, engineData).then(function (response) {
            // Success
            // 200 OK
            // 201 Created

            JobFactory.RemoveJob(job);

            $log.info("Engine " + name + " started successfully");

            if (typeof (successCallback) == "function") {
                successCallback();
            }


        }, function (response) {
            // Error
            JobFactory.RemoveJob(job);

            var errorHeader = "Failed to start application";
            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                var messageObject;
                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 404) {
                    // 404 A database with the specified name was not found.
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                }
                else if (response.status == 422) {
                    // 422 The application can not be found or The weaver failed to load a binary user code file.
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                }
                else if (response.status == 500) {
                    // 500 Server Error
                    errorHeader = "Internal Server Error";
                    messageObject = UtilsFactory.createServerErrorMessage(errorHeader, response.data);
                }
                else {
                    // Unhandle Error
                    messageObject = UtilsFactory.createServerErrorMessage(errorHeader, response.data);
                }


                errorCallback(messageObject);
            }

        });

    }


}]);