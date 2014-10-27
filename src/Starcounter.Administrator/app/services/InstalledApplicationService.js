/**
 * ----------------------------------------------------------------------------
 * Installed Applications Service
 * ----------------------------------------------------------------------------
 */
adminModule.service('InstalledApplicationService', ['$http', '$log', 'UtilsFactory', function ($http, $log, UtilsFactory) {

    var self = this;

    // List of applications
    //  {
    //      "ID" : "ABC123",
    //      "Namespace": "mycompany.myapp",
    //      "Name": "myapp",
    //      "Description" : "some description"
    //      ....
    //  }
    this.applications = [];

    /**
     * Get all installed applications
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this._getApplications = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrieve a list of installed applications";
        var uri = "/api/admin/installed/apps";

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

            $log.info("Installed Applications (" + response.data.Items.length + ") successfully retrived");
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
     * Get Installed Application
     * @param {string} databaseName Database name
     * @param {string} applicationName Application name
     * @return {object} Application or null
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
     * Refresh installed applications
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.refreshApplications = function (successCallback, errorCallback) {

        this._getApplications(function (installedApplications) {
            // Success

            // Update the current applications list with the new applications list
            self._updateInstalledApplicationsList(installedApplications);

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
     * Update current installed applications list with new list
     * @param {array} installedApplications New installed application list
     */
    this._updateInstalledApplicationsList = function (newInstalledApplications) {

        var newList = [];
        var removeList = [];

        // Check for new installed applications and update current installed application
        for (var i = 0; i < newInstalledApplications.length; i++) {
            var newInstalledApplication = newInstalledApplications[i];
            var installedApplication = this._getApplication(newInstalledApplication.ID);
            if (installedApplication == null) {
                newList.push(newInstalledApplication);
            } else {
                UtilsFactory.updateObject(newInstalledApplication, installedApplication, function (arg) {

                    //if (arg.propertyName == "running") {

                    //    if (arg.newValue) {
                    //        self._onDatabaseStarted(arg.source);
                    //    }
                    //    else {
                    //        self._onDatabaseStopped(arg.source);
                    //    }
                    //}


                });
            }
        }

        // Remove removed installed applications from installed application list
        for (var i = 0; i < self.applications.length; i++) {

            var installedApplication = self.applications[i];
            var bExists = false;
            // Check if it exist in newList
            for (var n = 0; n < newInstalledApplications.length; n++) {
                var newInstalledApplication = newInstalledApplications[n];
                if (installedApplication.ID == newInstalledApplication.ID) {
                    bExists = true;
                    break;
                }
            }

            if (bExists == false) {
                removeList.push(installedApplication);
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
     * On New installed application Event
     * @param {object} installedApplication installed application
     */
    this._onNewApplication = function (application) {
    }

    /**
     * On installed application Removed event
     * @param {object} installedApplication installed application
     */
    this._onRemovedApplication = function (application) {
    }


    /**
     * Start installed application
     * @param {object} application Installed application
     * @param {string} databaseName Database name
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.startApplication = function (application, databaseName, successCallback, errorCallback) {

        var uri = "/api/admin/installed/task";

        application.task = { "Text": "Starting" };

        var task = { "Type": "Start", "ID": application.ID, "DatabaseName" : databaseName };

        $http.post(uri, task).then(function (response) {

            // Success (202, 204)
            application.task = null;

            $log.info("App Store Application " + application.DisplayName + " was successfully started");

            if (typeof (successCallback) == "function") {
                successCallback();
            }

            self.refreshApplications();

        }, function (response) {

            application.task = null;

            // Error
            var errorHeader = "Failed to start App Store application";
            var messageObject;

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 403) {
                    // 403 forbidden
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
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

                self.refreshApplications();
            }
        });
    }
}]);