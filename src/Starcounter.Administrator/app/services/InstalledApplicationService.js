/**
 * ----------------------------------------------------------------------------
 * Applications Service
 * ----------------------------------------------------------------------------
 */
adminModule.service('InstalledApplicationService', ['$http', '$log', '$sce', 'ConsoleService', 'UtilsFactory', 'JobFactory', function ($http, $log, $sce, ConsoleService, UtilsFactory, JobFactory) {

    var self = this;

    // List of applications
    //  {
    //      "Namespace": "mycompany.myapp",
    //      "Name": "myapp",
    //      "Description" : "some description"
    //  }
    this.installedApplications = [];

    /**
     * Get all running applications
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.getInstalledApplications = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrieve a list of installed applications";
        var uri = "/api/admin/installed/applications";

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
    this.getInstalledApplication = function (nameSpace) {

        for (var i = 0 ; i < self.installedApplications.length ; i++) {
            if (self.installedApplications[i].Namespace == nameSpace) {
                return self.installedApplications[i];
            }
        }
        return null;
    }


    /**
     * Refresh installed applications
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.refreshInstalledApplications = function (successCallback, errorCallback) {

        this.getInstalledApplications(function (installedApplications) {
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
            var installedApplication = this.getInstalledApplication(newInstalledApplication.Namespace);
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
        for (var i = 0; i < self.installedApplications.length; i++) {

            var installedApplication = self.installedApplications[i];
            var bExists = false;
            // Check if it exist in newList
            for (var n = 0; n < newInstalledApplications.length; n++) {
                var newInstalledApplication = newInstalledApplications[n];

                if (installedApplication.Namespace == newInstalledApplication.Namespace) {
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
            var index = self.installedApplications.indexOf(removeList[i]);
            if (index > -1) {
                self.installedApplications.splice(index, 1);
            }
            this._onRemovedInstalledApplication(removeList[i]);
        }

        // Add new installed applications
        for (var i = 0; i < newList.length; i++) {
            self.installedApplications.push(newList[i]);
            this._onNewInstalledApplication(newList[i]);
        }

    }

    /**
     * On New installed application Event
     * @param {object} installedApplication installed application
     */
    this._onNewInstalledApplication = function (installedApplication) {
    }

    /**
     * On installed application Removed event
     * @param {object} installedApplication installed application
     */
    this._onRemovedInstalledApplication = function (installedApplication) {
    }

    /**
     * Get database
     * @param {string} databaseName Database name
     * @return {object} Database or null
     */
    this.getInstalledApplication = function (installedApplicicationNameSpace) {

        for (var i = 0 ; i < self.installedApplications.length ; i++) {
            if (self.installedApplications[i].Namespace == installedApplicicationNameSpace) {
                return self.installedApplications[i];
            }
        }
        return null;
    }


    /**
     * Delete installed application
     * @param {object} installedApplication Installed Application
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.deleteInstalledApplication = function (installedApplication, successCallback, errorCallback) {

        var job = { message: "Uninstalling installed application " + installedApplication.Namespace };

        JobFactory.AddJob(job);
        var uri = UtilsFactory.toRelativePath(installedApplication.Uri);

        $http.delete(uri).then(function (response) {

            // Success (202, 204)
            JobFactory.RemoveJob(job);
            $log.info("Installed application " + installedApplication.Namespace + " was successfully uninstalled");

            // TODO: Refresh applications (HostModelService)

            // Refresh databases
            self.refreshInstalledApplications(successCallback, errorCallback);

        }, function (response) {

            // Error
            JobFactory.RemoveJob(job);
            var errorHeader = "Failed to uninstall application";
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