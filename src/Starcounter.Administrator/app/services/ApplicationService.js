/**
 * ----------------------------------------------------------------------------
 * Applications Service
 * ----------------------------------------------------------------------------
 */
adminModule.service('ApplicationService', ['$http', '$log', '$sce', 'UtilsFactory', 'JobFactory', function ($http, $log, $sce, UtilsFactory, JobFactory) {

    var self = this;

    /**
     * Pick an executable by opening a filedialog on the server
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.pickExecutable = function (successCallback, errorCallback) {

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
     * Start Executable
     * @param {object} executable Executable
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.startLocalExecutable = function (application, successCallback, errorCallback) {

        application.task = { "Text": "Starting" };

        this._startEngine(application.databaseName, function () {
            // Success

            var bodyData = {
                "Uri": "",
                "Path": application.ApplicationFilePath,
                "ApplicationFilePath": application.ApplicationFilePath,
                "Name": application.Path.replace(/^.*[\\\/]/, ''),
                "Description": "",
                "Arguments": [],
                "DefaultUserPort": 0,
                "ResourceDirectories": [],
                "WorkingDirectory": application.WorkingDirectory,
                "AsyncEntrypoint": false,
                "TransactEntrypoint": false,
                "StartedBy": application.StartedBy,
                "Engine": { "Uri": "" },
                "RuntimeInfo": {
                    "LoadPath": "",
                    "Started": "",
                    "LastRestart": ""
                }
            };

            //application.task = { "Text": "Starting" };

            // Add job
//            var job = { message: "Starting application " + application.Name + " in " + application.databaseName };
//            JobFactory.AddJob(job);

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
                //JobFactory.RemoveJob(job);

                application.task = null;
                $log.info("Application " + response.data.Path + " was successfully started");

                // TODO: Return the started application
                if (typeof (successCallback) == "function") {
                    successCallback();
                }

            }, function (response) {
                // Error

                application.task = null;

//                JobFactory.RemoveJob(job);

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
            application.task = null;

            if (typeof (errorCallback) == "function") {
                errorCallback(errorMessageObject);
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
    this._startEngine = function (name, successCallback, errorCallback) {

        var errorHeader = "Failed to start database";

//        var job = { message: "Starting database " + name };
//        JobFactory.AddJob(job);

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

//            JobFactory.RemoveJob(job);

            $log.info("Engine " + name + " started successfully");

            if (typeof (successCallback) == "function") {
                successCallback();
            }


        }, function (response) {
            // Error
//            JobFactory.RemoveJob(job);

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