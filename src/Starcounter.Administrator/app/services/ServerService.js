/**
 * ----------------------------------------------------------------------------
 * Server Service
 * ----------------------------------------------------------------------------
 */
adminModule.service('ServerService', ['$http', '$log', '$rootScope', 'UtilsFactory', 'JobFactory', function ($http, $log, $rootScope, UtilsFactory, JobFactory) {

    this.model = {
        settings: null
    }

    var self = this;


    /**
     * Get Server settings
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.getServerSettings = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrieve the server settings";
        var uri = "/api/admin/servers/personal/settings";

        // Response
        // {
        //     name:"default",
        //     httpPort:8080,
        //     version:"2.0.0.0"
        // }
        $http.get(uri).then(function (response) {
            // success handler

            $log.info("Server settings successfully retrived");
            if (typeof (successCallback) == "function") {
                successCallback(response.data);
            }

        }, function (response) {

            // Error
            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                var messageObject;

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 404) {
                    // 404 Not found
                    errorHeader = "Server not found";
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
     * Refresh Server Settings
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.refreshServerSettings = function (successCallback, errorCallback) {

        this.getServerSettings(function (settings) {
            // Success

            // TODO: Update current properties with new values
            //       instead of replacing the settings object

            // Clear database list
            self.model.settings = settings

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
     * Save server settings
     * @param {object} settings Settings
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.saveSettings = function (settings, successCallback, errorCallback) {

        var errorHeader = "Failed to save server settings";

//        var job = { message: "Saving server settings" };
//        JobFactory.AddJob(job);

        $http.put('/api/admin/servers/personal/settings', settings).then(function (response) {

            // success
//            JobFactory.RemoveJob(job);

            if (successCallback != null) {
                successCallback(response.data);
            }

        }, function (response) {

            // Error
//            JobFactory.RemoveJob(job);
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
     * Get available collations
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.getCollations = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrieve the server settings";
        var uri = "/api/admin/servers/personal/collationfiles";

        // Get a list of all running Executables
        // Example response
        //{
        // "Items": [
        //      {
        //          "File": "http://example.com/api/executables/foo/foo.exe-123456789",
        //          "Description": "Swedish",
        //      }
        //  ]
        //}
        $http.get(uri).then(function (response) {
            // success handler

            $log.info("Server collations (" + response.data.Items.length + ") successfully retrived");

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
