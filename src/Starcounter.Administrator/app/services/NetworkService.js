/**
 * ----------------------------------------------------------------------------
 * Network Service
 * ----------------------------------------------------------------------------
 */
adminModule.service('NetworkService', ['$http', '$sce', '$log', '$location', 'UtilsFactory', 'HostModelService', function ($http, $sce, $log, $location, UtilsFactory, HostModelService) {

    // Network model
    // {
    //    statistics : object,
    //    workingfolders : [{"Port":8080, "Folder":"folderpath"}],
    //    uriAliases : [{"HttpMethod":"GET", "FromUri":"/", "ToUri":"/launcher", "Port":7000}],
    //    reverseProxies: [{"DestinationIP":"127.0.0.1", "DestinationPort":80, "StarcounterProxyPort":7000, "MatchingHost":"mydomain.com"}],
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
    this.getNetworkWorkingFolders = function (port, successCallback, errorCallback) {

        var errorHeader = "Failed to retrieve the Network working folders on port " + port;

        var uri = $location.protocol() + "://" + $location.host() + ":" + port + "/staticcontentdir";

        $http.get(uri).then(function (response) {
            // Success
            $log.info("Network web folders (" + response.data.Items.length + ") successfully retrived");
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
       * Get Uri Aliases
       * @param {function} successCallback Success Callback function
       * @param {function} errorCallback Error Callback function
       */
    this.getUriAliases = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrieve Uri Aliases";
        var uri = "/sc/alias";

        $http.get(uri).then(function (response) {
            // Success
            $log.info("Uri Aliases successfully retrived");
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
       * Get Reverse Proxies
       * @param {function} successCallback Success Callback function
       * @param {function} errorCallback Error Callback function
       */
    this.getReverseProxies = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrieve Reverse Proxies";
        var uri = "/sc/reverseproxies";

        $http.get(uri).then(function (response) {
            // Success
            $log.info("Reverse Proxies successfully retrived");
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
     * Refresh Network Statistics
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.refreshNetworkStatistics = function (successCallback, errorCallback) {

        this.getUriAliases(function (uriAliases) {

            self.model.uriAliases = uriAliases.Items;

        }, errorCallback);

        this.getReverseProxies(function (reverseProxies) {

            self.model.reverseProxies = reverseProxies.Items;

        }, errorCallback);



        this.getNetworkStatistics(function (statistics) {
            // Success
            self.model.statistics = statistics;

            // Get Static folders per database
            var databasesToProcess = [];
            self.model.workingfolders = [];
            var databases = HostModelService.data.model.Databases
            for (var i = 0 ; i < databases.length ; i++) {
                var database = databases[i];

                for (var n = 0 ; n < database.Applications.length ; n++) {
                    if (database.Applications[n].IsRunning) {
                        databasesToProcess.push(databases[i]);
                        break;
                    }
                }
            
            }
            self.getNetworkStaticFolders(databasesToProcess, successCallback, errorCallback);

        }, function (messageObject) {
            // Error

            if (typeof (errorCallback) == "function") {
                errorCallback(messageObject);
            }

        });
    }


    this.getNetworkStaticFolders = function (databasesToProcess, successCallback, errorCallback) {

        if (databasesToProcess.length == 0) {
            if (typeof (successCallback) == "function") {
                successCallback();
            }
            return;
        }

        var database = databasesToProcess.pop();
        self.getNetworkWorkingFolders(database.UserHttpPort, function (workingFolders) {

            // Success
            for (var i = 0 ; i < workingFolders.length; i++) {
                self.model.workingfolders.push(workingFolders[i]);
            }

            self.getNetworkStaticFolders(databasesToProcess, successCallback, errorCallback);

        }, function (messageObject) {
            // Error

            if (typeof (errorCallback) == "function") {
                errorCallback(messageObject);
            }
        });
    }
}]);
