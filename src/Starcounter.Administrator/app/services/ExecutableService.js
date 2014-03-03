/**
 * ----------------------------------------------------------------------------
 * Executables Service
 * ----------------------------------------------------------------------------
 */
adminModule.service('ExecutableService', ['$http', '$log', '$sce', 'ConsoleService', 'UtilsFactory', 'JobFactory', function ($http, $log, $sce, ConsoleService, UtilsFactory, JobFactory) {

    var self = this;

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
    //
    //      databaseName : "default",
    //      key : "foo.exe-123456789",
    //      console : "console output",
    //      consoleManualMode : false
    //  }
    this.executables = [];


    /**
     * Get all running executables
     * @param {successCallback} successCallback function
     * @param {errorCallback} errorCallback function
     */
    this.getExecutables = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrive a list of executables";
        var uri = "/api/admin/executables";

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

            $log.info("Executables (" + response.data.Items.length + ") successfully retrived");
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
     * Get executable
     * @param {executableName} executableName Executable name
     * @return {executable} Executable or null
     */
    this.getExecutable = function (executableName) {

        for (var i = 0 ; i < self.executables.length ; i++) {
            if (self.executables[i].Name == executableName) {
                return self.executables[i];
            }
        }
        return null;
    }


    /**
     * Refresh executables
     * @param {successCallback} successCallback function
     * @param {errorCallback} errorCallback function
     */
    this.refreshExecutables = function (successCallback, errorCallback) {

        this.getExecutables(function (executables) {
            // Success

            self._updateExecutableList(executables);

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
     * Refresh executable console output
     * @param {successCallback} successCallback function
     * @param {errorCallback} errorCallback function
     */
    this.refreshConsoleOuput = function (executable, successCallback, errorCallback) {

        var filter = executable.Name;

        ConsoleService.getConsoleOuput(executable.databaseName, filter, function (text) {
            // Success

            self._onConsoleOutputEvent(executable, text, false);

            if (typeof (successCallback) == "function") {
                successCallback();
            }

        }, errorCallback);

    }


    /**
     * Callback when there is an incoming console message
     * @param {executable} executable
     * @param {text} text
     * @param {bAppend} bAppend
     */
    this._onConsoleOutputEvent = function (executable, text, bAppend) {

        var htmlText = text.replace(/\r\n/g, "<br>");

        if (bAppend) {
            executable.console = $sce.trustAsHtml(executable.console + htmlText);
        }
        else {
            executable.console = $sce.trustAsHtml(htmlText);
        }

        // Limit the buffer
        if (executable.console.length > self.bufferSize) {
            executable.console = $sce.trustAsHtml(executable.console.substr(executable.console.length - self.bufferSize));
        }

    }


    /**
     * Update current executable list with new list
     * @param {newExecutables} New Executable list
     */
    this._updateExecutableList = function (newExecutables) {

        var newList = [];
        var removeList = [];

        // Check for new executabels and update current executables
        for (var i = 0; i < newExecutables.length; i++) {
            var newExecutable = newExecutables[i];
            var executable = this.getExecutable(newExecutable.Name);
            if (executable == null) {
                newList.push(newExecutable);
            } else {
                UtilsFactory.updateObject(newExecutable, executable);
            }
        }

        // Remove removed executables from executable list
        for (var i = 0; i < self.executables.length; i++) {

            var executable = self.executables[i];
            var bExists = false;
            // Check if it exist in newList
            for (var i = 0; i < newExecutables.length; i++) {
                var newExecutable = newExecutables[i];

                if (executable.Name == newExecutable.Name) {
                    bExists = true;
                    break;
                }
            }

            if (bExists == false) {
                removeList.push(executable);
            }
        }


        // Remove executable from executable list
        for (var i = 0; i < removeList.length; i++) {
            var index = self.executables.indexOf(removeList[i]);
            if (index > -1) {
                self.executables.splice(index, 1);
            }
            this._onRemovedExecutable(executable);
        }

        // Add new executables
        for (var i = 0; i < newList.length; i++) {
            self.executables.push(newList[i]);
            this._onNewExecutable(newList[i]);
        }

    }


    /**
     * On New Executable Event
     * @param {executable} Executable
     */
    this._onNewExecutable = function (executable) {

        // Add additional properties
        executable.databaseName = executable.Engine.Uri.replace(/^.*[\\\/]/, '')
        executable.key = executable.Uri.replace(/^.*[\\\/]/, '')
        executable.console = "";
        executable.consoleManualMode = false;

        // Socket event listener
        executable.consoleListener = {
            databaseName: executable.databaseName,
            onEvent: function (text) {
                self._onConsoleOutputEvent(executable, text, true);
            },
            onError: function (messageObject) {

                // Sliently fallback to manual mode
                executable.consoleManualMode = true;

            },
            filter: executable.Name
        }

        ConsoleService.registerEventListener(executable.consoleListener);
    }


    /**
     * On Executable Removed event
     * @param {executable} Executable
     */
    this._onRemovedExecutable = function (executable) {
        ConsoleService.unregisterEventListener(executable.consoleListener);
    }


    /**
     * Restart Executable
     * @param {Executable} executable
     * @param {successCallback} successCallback function
     * @param {errorCallback} errorCallback function
     */
    this.restartExecutable = function (executable, successCallback, errorCallback) {

        this.stopExecutable(executable, function () {
            // Success
            self.startExecutable(executable.Path, executable.databaseName, successCallback, errorCallback)

        }, function (messageObject) {
            // Error
            if (typeof (errorCallback) == "function") {
                errorCallback(messageObject);
            }
        })
    }


    /**
     * Start Executable
     * @param {Executable} executable
     * @param {successCallback} successCallback function
     * @param {errorCallback} errorCallback function
     */
    this.startExecutable = function (file, databaseName, successCallback, errorCallback) {

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
            var job = { message: "Starting executable " + file + " in " + databaseName };
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

                $log.info("Executable " + response.data.Path + " was successfully started");

                // Refresh executables
                self.refreshExecutables(successCallback, errorCallback);

                // TODO: Return the started executable

            }, function (response) {
                // Error
                JobFactory.RemoveJob(job);

                var errorHeader = "Failed to start executable";
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
                        // 409 The executable is already running or the Engine is not started.
                        messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                    }
                    else if (response.status == 422) {
                        // 422 The executable can not be found or The weaver failed to load a binary user code file.
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
     * Stop Executable
     * @param {Executable} executable
     * @param {successCallback} successCallback function
     * @param {errorCallback} errorCallback function
     */
    this.stopExecutable = function (executable, successCallback, errorCallback) {

        var job = { message: "Stopping executable " + executable.Name };
        JobFactory.AddJob(job);

        var uri = UtilsFactory.toRelativePath(executable.Uri);

        $http.delete(uri).then(function (response) {
            // Success, 204 No Content 
            JobFactory.RemoveJob(job);

            $log.info("Executable " + executable.Name + " was successfully stopped");

            // Refresh databases
            self.refreshExecutables(successCallback, errorCallback);

        }, function (response) {
            JobFactory.RemoveJob(job);

            // Error
            var errorHeader = "Failed to stop executable";
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
                    // 409 The executable is already running or the Engine is not started.
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
     * @param {name} name
     * @return {Promise} promise
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

            var errorHeader = "Failed to start executable";
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
                    // 422 The executable can not be found or The weaver failed to load a binary user code file.
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