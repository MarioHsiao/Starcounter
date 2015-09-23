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

        $http.post('/api/admin/databases', settings).then(function (response) {

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

    /**
     * Get Database default settings
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.getDatabaseDefaultSettings = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrieve the database default settings";

        $http.get('/api/admin/settings/database').then(function (response) {
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
}]);
