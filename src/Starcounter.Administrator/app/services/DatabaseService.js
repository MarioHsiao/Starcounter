/**
 * ----------------------------------------------------------------------------
 * Databases Service
 * ----------------------------------------------------------------------------
 */
adminModule.service('DatabaseService', ['$http', '$log', 'UtilsFactory', 'JobFactory', function ($http, $log, UtilsFactory, JobFactory) {

    var self = this;

    /**
     * Create database
     * @param {object} settings Settings
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.createDatabase = function (settings, successCallback, errorCallback) {

        var uri = "/api/admin/databases";
        var self = this;

        var req = {
            method: 'POST',
            url: uri,
            transformRequest: [function (data) {

                var jsonStr = angular.toJson(data);
                var result = self.FixStringToNumber("FirstObjectID", jsonStr);
                return self.FixStringToNumber("LastObjectID", result);
            }],
            data: settings
        }

        //        $http.post('/api/admin/databases', settings).then(function (response) {

        $http(req).then(function (response) {

            // success handler
            if (successCallback != null) {
                // TODO: Return the newly create database
                successCallback(response.data.ID);
            }
        }, function (response) {

            // Error
            var messageObject;
            var errorHeader = "Failed to create database";

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 403) {
                    // 403 forbidden (Validation errors)
                    if (response.data.hasOwnProperty("Items") == true) {
                        // Validation errors
                        errorCallback(null, response.data.Items);
                        return;
                    }
                    // TODO:
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data, response.data.Helplink);

                }
                else if (response.status == 404) {
                    // 404 Not found
                    // The database was not created
                    messageObject = UtilsFactory.createServerErrorMessage(errorHeader, response.data);
                }
                else if (response.status == 422) {
                    // 422 Unprocessable Entity (WebDAV; RFC 4918)
                    // The request was well-formed but was unable to be followed due to semantic errors
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
     * Get Database settings
     * @param {object} database Database
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.getSettings = function (database, successCallback, errorCallback) {

        var uri = "/api/admin/databases/" + database.ID + "/settings";

        $http.get(uri).then(function (response) {
            // success handler

            $log.info("Databases settings successfully retrived");
            if (typeof (successCallback) == "function") {
                successCallback(response.data);
            }

        }, function (response) {

            // Error
            var messageObject;

            var errorHeader = "Failed to retrieve the database settings";

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

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

    this.FixStringToNumber = function (propertyName, data) {

        var propertyIndex = data.indexOf("\"" + propertyName + "\"");
        if (propertyIndex == -1) return data;

        var colonIndex = data.indexOf(":", propertyIndex + propertyName.length + 2);
        if (colonIndex == -1) return data;
        colonIndex++;

        var startIndex = data.indexOf("\"", colonIndex);
        if (startIndex == -1) return data;

        var output = [data.slice(0, startIndex), data.slice(startIndex + 1)].join('');

        var endIndex = output.indexOf("\"", startIndex);
        if (endIndex == -1) return data;

        return [output.slice(0, endIndex), output.slice(endIndex + 1)].join('');
    }
    this.FixNumberToString = function (propertyName, data) {

        var propertyIndex = data.indexOf("\"" + propertyName + "\"");
        if (propertyIndex == -1) return data;

        var colonIndex = data.indexOf(":", propertyIndex + propertyName.length + 2);
        if (colonIndex == -1) return data;
        colonIndex++;

        var output = [data.slice(0, colonIndex), "\"", data.slice(colonIndex)].join('');

        var endIndex = output.indexOf(",", colonIndex);
        if (endIndex == -1) {
            var endIndex = output.indexOf("}", colonIndex);
        }

        if (endIndex == -1) return data;

        return [output.slice(0, endIndex), "\"", output.slice(endIndex)].join('');
    }

    /**
     * Get Database default settings
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.getDatabaseDefaultSettings = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrieve the database default settings";

        var self = this;
        var req = {
            method: 'GET',
            url: '/api/admin/settings/database',
            transformResponse: [function (data) {
                var result = self.FixNumberToString("FirstObjectID", data);
                result = self.FixNumberToString("LastObjectID", result);
                return JSON.parse(result);
            }]
        }

        $http(req).then(function (response) {
            // success handler
            $log.info("Default Databases settings successfully retrived");
            if (typeof (successCallback) == "function") {
                successCallback(response.data);
            }

        }, function (response) {

            // Error
            var messageObject;

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

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
     * Save database settings
     * @param {object} database Database
     * @param {object} settings Settings
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.saveSettings = function (database, settings, successCallback, errorCallback) {

        var errorHeader = "Failed to save database settings";

        var uri = "/api/admin/databases/" + database.ID + "/settings";

        $http.put(uri, settings).then(function (response) {

            if (successCallback != null) {
                successCallback(response.data);
            }

        }, function (response) {

            // Error
            var messageObject;

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 403) {
                    // 403 forbidden (Validation errors)

                    // Validation errors
                    if (response.data.hasOwnProperty("Items") == true) {
                        errorCallback(null, response.data.Items);
                        return;
                    }

                    // TODO:
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data, response.data.Helplink);


                }
                else if (response.status == 404) {
                    // 404 Not found
                    // The database was not created
                    messageObject = UtilsFactory.createServerErrorMessage(errorHeader, response.data);
                }
                    //else if (response.status == 422) {
                    //    // 422 Unprocessable Entity (WebDAV; RFC 4918)
                    //    // The request was well-formed but was unable to be followed due to semantic errors
                    //    messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                    //}
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
