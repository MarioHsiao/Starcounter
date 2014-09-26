/**
 * ----------------------------------------------------------------------------
 * Applications Service
 * ----------------------------------------------------------------------------
 */
adminModule.service('AppStoreService', ['$http', '$log', '$sce', '$rootScope', 'ConsoleService', 'UtilsFactory', 'JobFactory', 'InstalledApplicationService', function ($http, $log, $sce, $rootScope, ConsoleService, UtilsFactory, JobFactory, InstalledApplicationService) {

    var self = this;

    this.appStoreApplications = [];

    this.appStoreService = { "Enabled": false };

    /**
     * Get all running applications
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.getApplications = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrieve a list of AppStore applications";
        var uri = "/api/admin/appstore/apps";


        // Example JSON response 
        //-----------------------
        //{
        //    "Items":[{
        //      "Namespace": "mycompany.myapp",
        //      "Name": "myapp",
        //      "Description" : "some description"
        //    }]
        //}
        $http.get(uri).then(function (response) {
            // Success
            self.appStoreService.Enabled = true;

            $log.info("AppStore Applications (" + response.data.Items.length + ") successfully retrived");
            if (typeof (successCallback) == "function") {
                successCallback(response.data.Items);
            }

        }, function (response) {
            // Error

            self.appStoreService.Enabled = false;
            self.appStoreApplications.length = 0;

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {
                var messageObject;

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 503) {
                    // 500 Server Error
                    errorHeader = "AppStore Service unavailable";
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
     * Get AppStore Application
     * @param {string} nameSpace AppStore Application namespace
     * @return {object} Application or null
     */
    this.getApplication = function (nameSpace) {

        for (var i = 0 ; i < self.appStoreApplications.length ; i++) {
            if (self.appStoreApplications[i].Namespace == nameSpace) {
                return self.appStoreApplications[i];
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
     * Update current local AppStore applications list with new list
     * @param {array} newApplications New AppStore application list
     */
    this._updateApplicationsList = function (newApplications) {

        var newList = [];
        var removeList = [];

        // Check for new installed applications and update current installed application
        for (var i = 0; i < newApplications.length; i++) {
            var newAppStoreApplication = newApplications[i];
            var appStoreApplication = this.getApplication(newAppStoreApplication.Namespace);
            if (appStoreApplication == null) {
                newList.push(newAppStoreApplication);
            } else {
                UtilsFactory.updateObject(newAppStoreApplication, appStoreApplication, function (arg) { });
            }
        }


        // Remove removed installed applications from installed application list
        for (var i = 0; i < self.appStoreApplications.length; i++) {

            var appStoreApplication = self.appStoreApplications[i];
            var bExists = false;
            // Check if it exist in newList
            for (var n = 0; n < newApplications.length; n++) {
                var newAppStoreApplication = newApplications[n];

                if (appStoreApplication.Namespace == newAppStoreApplication.Namespace) {
                    bExists = true;
                    break;
                }
            }

            if (bExists == false) {
                removeList.push(appStoreApplication);
            }
        }

        // Remove installed applications from installed applications list
        for (var i = 0; i < removeList.length; i++) {
            var index = self.appStoreApplications.indexOf(removeList[i]);
            if (index > -1) {
                self.appStoreApplications.splice(index, 1);
            }
            this._onRemovedAppStoreApplication(removeList[i]);
        }

        // Add new installed applications
        for (var i = 0; i < newList.length; i++) {
            self.appStoreApplications.push(newList[i]);
            this._onNewAppStoreApplication(newList[i]);
        }

    }

    /**
     * On New AppStore application Event
     * @param {object} appStoreApplication AppStore application
     */
    this._onNewAppStoreApplication = function (appStoreApplication) {
    }

    /**
     * On AppStore application Removed event
     * @param {object} appStoreApplication AppStore application
     */
    this._onRemovedAppStoreApplication = function (installedApplication) {
    }

    /**
     * Install AppStore application
     * @param {object} appStoreApplication AppStore application
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.installApplication = function (appStoreApplication, successCallback, errorCallback) {

        var job = { message: "Installing AppStore application " + appStoreApplication.Namespace };

        JobFactory.AddJob(job);
//        var uri = UtilsFactory.toRelativePath(installedApplication.Uri);
        var uri = "/api/admin/installed/applications";

        $http.post(uri, appStoreApplication).then(function (response) {

            // Success (202, 204)
            JobFactory.RemoveJob(job);
            $log.info("AppStore Application " + appStoreApplication.Namespace + " was successfully installed");

            // Refresh
            InstalledApplicationService.refreshInstalledApplications(successCallback, errorCallback);

        }, function (response) {

            // Error
            JobFactory.RemoveJob(job);
            var errorHeader = "Failed to install AppStore application";
            var messageObject;

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 404) {
                    // 404 A database with the specified name was not found.
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                }
                else if (response.status == 409) {
                    // 409 The database is already running or the Engine is not started.
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