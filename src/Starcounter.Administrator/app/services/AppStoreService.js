/**
 * ----------------------------------------------------------------------------
 * Applications Service
 * ----------------------------------------------------------------------------
 */
adminModule.service('AppStoreService', ['$http', '$rootScope', '$log', '$sce', 'ConsoleService', 'UtilsFactory', 'JobFactory', function ($http, $rootScope, $log, $sce, ConsoleService, UtilsFactory, JobFactory) {

    var self = this;

    this.applications = [];

    this.status = { "Enabled": false };

    /**
     * Get App Store applications
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this._getApplications = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrieve a list of App Store applications";
        var uri = "/api/admin/appstore/apps";

        $http.get(uri).then(function (response) {
            // Success

            self.status.Enabled = true;

            $log.info("App Store Applications (" + response.data.Items.length + ") successfully retrived");
            if (typeof (successCallback) == "function") {
                successCallback(response.data.Items);
            }
        }, function (response) {
            // Error

            self.status.Enabled = false;

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {
                var messageObject;

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 503) {
                    // 500 Server Error
                    errorHeader = "App Store Service unavailable";
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
     * Get a App Store Application
     * @param {string} id App Store Application ID
     * @return {object} App Store Application or null
     */
    this._getApplication = function (id) {

        for (var i = 0 ; i < self.applications.length ; i++) {
            if (self.applications[i].ID == id) {
                return self.applications[i];
            }
        }
        return null;
    }

    /**
     * Refresh App Store applications
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.refreshApplications = function (successCallback, errorCallback) {

        this._getApplications(function (applications) {
            // Success

            // Update the current applications list with the new applications list
            self._updateApplicationsList(applications);

            if (typeof (successCallback) == "function") {
                successCallback();
            }
        }, function (response) {
            // Error
            self.applications.length = 0;

            if (typeof (errorCallback) == "function") {
                errorCallback(response);
            }
        });
    }

    /**
     * Update current local App Store applications list with new list
     * @param {array} newApplications New AppStore application list
     */
    this._updateApplicationsList = function (newApplications) {

        var newList = [];
        var removeList = [];

        // Check for new installed applications and update current installed application
        for (var i = 0; i < newApplications.length; i++) {

            var newAppStoreApplication = newApplications[i];
            var appStoreApplication = this._getApplication(newAppStoreApplication.ID);
            if (appStoreApplication == null) {
                newList.push(newAppStoreApplication);
            } else {
                UtilsFactory.updateObject(newAppStoreApplication, appStoreApplication, function (arg) { });
            }
        }

        // Remove removed installed applications from installed application list
        for (var i = 0; i < self.applications.length; i++) {

            var appStoreApplication = self.applications[i];
            var bExists = false;
            // Check if it exist in newList
            for (var n = 0; n < newApplications.length; n++) {
                var newAppStoreApplication = newApplications[n];

                if (appStoreApplication.ID == newAppStoreApplication.ID) {
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
            var index = self.applications.indexOf(removeList[i]);
            if (index > -1) {
                self.applications.splice(index, 1);
            }
            this._onRemovedApplication(removeList[i]);
        }

        // Add new installed applications
        for (var i = 0; i < newList.length; i++) {
            self.applications.push(newList[i]);
            this._onNewApplication(newList[i]);
        }
    }

    /**
     * On New AppStore application Event
     * @param {object} appStoreApplication AppStore application
     */
    this._onNewApplication = function (appStoreApplication) {
    }

    /**
     * On AppStore application Removed event
     * @param {object} appStoreApplication AppStore application
     */
    this._onRemovedApplication = function (installedApplication) {
    }

    /**
     * Install AppStore application
     * @param {object} application App Store application
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.install = function (application, successCallback, errorCallback) {

        var uri = "/api/admin/installed/apps";

        application.task = { "Text": "Installing" };

        // Clone application instance so we can remove unsupported properties
        // Make the posted data match the server expected data format
        var appData = JSON.parse(JSON.stringify(application));
        delete appData.task;

        $http.post(uri, appData).then(function (response) {

            // Success (202, 204)
            application.task = null;

            $log.info("App Store Application " + application.DisplayName + " was successfully installed");

            if (typeof (successCallback) == "function") {
                successCallback();
            }

            self.refreshApplications();

        }, function (response) {

            application.task = null;

            // Error
            var errorHeader = "Failed to install App Store application";
            var messageObject;

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 404) {
                    // 404 A database with the specified name was not found.

                    if (response.data.Text) {
                        messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                    } else {
                        messageObject = UtilsFactory.createMessage(errorHeader, response.data);
                    }
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

    /**
     * Uninstall installed App Store application
     * @param {object} application App Store application
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.uninstall = function (application, successCallback, errorCallback) {

        application.task = { "Text": "Uninstalling" };

        var uri = "/api/admin/installed/apps/" + application.ID;

        $http.delete(uri).then(function (response) {

            application.task = null;
            // Success (202, 204)
            $log.info("Application " + application.DisplayName + " was successfully uninstalled");

            if (typeof (successCallback) == "function") {
                successCallback();
            }

            self.refreshApplications();

        }, function (response) {

            application.task = null;

            // Error
            var errorHeader = "Failed to uninstall application";
            var messageObject;

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 404) {
                    // 404 An app with the specified name was not found.
                    if (response.data.Text) {
                        messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                    } else {
                        messageObject = UtilsFactory.createMessage(errorHeader, response.data);
                    }
                }
                else if (response.status == 409) {
                    // 409 The application is already running or the Engine is not started.
                    messageObject = UtilsFactory.createMessage(errorHeader, "The installed application could not be found");
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