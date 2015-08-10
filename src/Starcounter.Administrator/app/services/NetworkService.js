/**
 * ----------------------------------------------------------------------------
 * Network Service
 * ----------------------------------------------------------------------------
 */
adminModule.service('NetworkService', ['$http', '$sce', '$log', 'UtilsFactory', function ($http, $sce, $log, UtilsFactory) {

    // Network model
    // {
    //    statistics : object,
    //    workingfolders : [{Port:8080, Folder:"folderpath"}]
    // }
    this.model = {};

    var self = this;

    /**
     * Get Network Statistics
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.getNetworkStatistics = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrieve the Network statistics";
        var uri = "/gw/stats";

        $http.get(uri).then(function (response) {
            // Success
            $log.info("Network statistics successfully retrived");
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
     * Get Network Registered "Static" Working folders
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.getNetworkWorkingFolders = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrieve the Network working folders";
        var uri = "/staticcontentdir";

        $http.get(uri).then(function (response) {
            // Success
            $log.info("Network working folders successfully retrived");
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


    /**
     * Refresh Network Statistics
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.refreshNetworkStatistics = function (successCallback, errorCallback) {

        this.getNetworkStatistics(function (statistics) {
            // Success
            self.model.statistics = statistics;

            self.getNetworkWorkingFolders(function (workingFolders) {

                // Success
                self.model.workingfolders = workingFolders;

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
    }
}]);
