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
    //      key : "foo.exe-123456789",
    //      console : "console output",
    //      consoleManualMode : false
    //  }
    this.applications = [];


    /**
     * Get all running applications
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.getApplications = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrive a list of applications";
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
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.message, response.data.helplink, response.data.stackTrace);
                }
                else {
                    // Unhandle Error
                    if (response.data.hasOwnProperty("Text") == true) {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.Text, response.data.Helplink, null);
                    }
                    else {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data, null, null);
                    }
                }

                errorCallback(messageObject);
            }

        });
    }


    /**
     * Get Application
     * @param {string} applicationName Application name
     * @return {object} Application or null
     */
    this.getApplication = function (applicationName) {

        for (var i = 0 ; i < self.applications.length ; i++) {
            if (self.applications[i].Name == applicationName) {
                return self.applications[i];
            }
        }
        return null;
    }


    /**
     * Refresh applications
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.refreshApplications = function (successCallback, errorCallback) {

        this.getApplications(function (applications) {
            // Success

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

        var htmlText = text.replace(/\r\n/g, "<br>");

        if (bAppend) {
            application.console = $sce.trustAsHtml(application.console + htmlText);
        }
        else {
            application.console = $sce.trustAsHtml(htmlText);
        }

        // Limit the buffer
        if (application.console.length > self.bufferSize) {
            application.console = $sce.trustAsHtml(application.console.substr(application.console.length - self.bufferSize));
        }

    }


    /**
     * Update current application list with new list
     * @param {array} newDatabases New application list
     */
    this._updateApplicationsList = function (newApplications) {

        var newList = [];
        var removeList = [];

        // Check for new executabels and update current applications
        for (var i = 0; i < newApplications.length; i++) {
            var newApplication = newApplications[i];
            var application = this.getApplication(newApplication.Name);
            if (application == null) {
                newList.push(newApplication);
            } else {
                UtilsFactory.updateObject(newApplication, application);
            }
        }

        // Remove removed applications from application list
        for (var i = 0; i < self.applications.length; i++) {

            var application = self.applications[i];
            var bExists = false;
            // Check if it exist in newList
            for (var i = 0; i < newApplications.length; i++) {
                var newApplication = newApplications[i];

                if (application.Name == newApplication.Name) {
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
            this._onRemovedApplication(application);
        }

        // Add new applications
        for (var i = 0; i < newList.length; i++) {
            self.applications.push(newList[i]);
            this._onNewApplication(newList[i]);
        }

    }


    /**
     * On New Application Event
     * @param {object} application Application
     */
    this._onNewApplication = function (application) {

        // Add additional properties
        application.databaseName = application.Engine.Uri.replace(/^.*[\\\/]/, '')
        application.key = application.Uri.replace(/^.*[\\\/]/, '')
        application.console = "";
        application.consoleManualMode = false;

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
     * On Application Removed event
     * @param {object} application Application
     */
    this._onRemovedApplication = function (application) {
        ConsoleService.unregisterEventListener(application.consoleListener);
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
            self.startApplication(application.Path, application.databaseName, successCallback, errorCallback)

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
    this.startApplication = function (file, databaseName, successCallback, errorCallback) {

        this.startEngine(databaseName, function () {
            // Success

            var bodyData = {
                "Uri": "",
                "Path": file,
                "ApplicationFilePath": file,
                "Name": file.replace(/^.*[\\\/]/, ''),
                "Description": "",
                "Arguments": [{
                    "dummy": file
                }],
                "DefaultUserPort": 0,
                "ResourceDirectories": [],
                "WorkingDirectory": null,
                "IsTool": true,
                "StartedBy": "Starcounter Administrator",
                "Engine": { "Uri": "" },
                "RuntimeInfo": {
                    "LoadPath": "",
                    "Started": "",
                    "LastRestart": ""
                }
            };

            // Add job
            var job = { message: "Starting application " + file + " in " + databaseName };
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
            $http.post('/api/engines/' + databaseName + '/executables', bodyData).then(function (response) {
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
                        if (response.data.hasOwnProperty("Text") == true) {
                            messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.Text, response.data.Helplink, null);
                        } else {
                            messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data, null, null);
                        }

                    }
                    else {
                        // Unhandle Error
                        if (response.data.hasOwnProperty("Text") == true) {
                            messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.Text, response.data.Helplink, null);
                        } else {
                            messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data, null, null);
                        }
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
                    if (response.data.hasOwnProperty("Text") == true) {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.Text, response.data.Helplink, null);
                    } else {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data, null, null);
                    }
                }
                else {
                    // Unhandle Error
                    if (response.data.hasOwnProperty("Text") == true) {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.Text, response.data.Helplink, null);
                    } else {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data, null, null);
                    }
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
                    if (response.data.hasOwnProperty("Text") == true) {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.Text, response.data.Helplink, null);
                    } else {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data, null, null);
                    }
                }
                else {
                    // Unhandle Error
                    if (response.data.hasOwnProperty("Text") == true) {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.Text, response.data.Helplink, null);
                    } else {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data, null, null);
                    }
                }


                errorCallback(messageObject);
            }

        });

    }


}]);