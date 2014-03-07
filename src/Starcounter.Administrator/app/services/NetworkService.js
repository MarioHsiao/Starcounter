/**
 * ----------------------------------------------------------------------------
 * Network Service
 * ----------------------------------------------------------------------------
 */
adminModule.service('NetworkService', ['$http', '$sce', '$log', 'UtilsFactory', function ($http, $sce, $log, UtilsFactory) {

    // Network model
    // {
    //    statistics : "<p>html text</p>";
    // }
    this.model = {};

    var self = this;

    /**
     * Get Network Statistics
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.getNetworkStatistics = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrive Network statistics";
        var uri = "/gwstats";

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
     * Refresh Network Statistics
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.refreshNetworkStatistics = function (successCallback, errorCallback) {

        this.getNetworkStatistics(function (statistics) {
            // Success
            self.model.statistics = statistics;
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


}]);
