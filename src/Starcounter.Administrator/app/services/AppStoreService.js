/**
 * ----------------------------------------------------------------------------
 * Applications Service
 * ----------------------------------------------------------------------------
 */
adminModule.service('AppStoreService', ['$http', '$rootScope', '$log', '$sce', 'ConsoleService', 'UtilsFactory', 'JobFactory', function ($http, $rootScope, $log, $sce, ConsoleService, UtilsFactory, JobFactory) {

    var self = this;

    this.stores = [];

    this.status = { "Enabled": false, "Updates" : 0 };

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
            self.status.Updates = response.data.Updates;

            for (var i = 0 ; i < response.data.Stores.length ; i++) {
                $log.info("App Store Applications (" + response.data.Stores[i].Items.length + ") successfully retrived from " + response.data.Stores[i].DisplayName);
            }

            if (typeof (successCallback) == "function") {
                successCallback(response.data.Stores);
            }
        }, function (response) {
            // Error

            self.status.Enabled = false;
            self.status.Updates = 0;

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
    this._getApplication = function (store, id) {

        for (var i = 0 ; i < store.Items.length ; i++) {
            if (store.Items[i].ID == id) {
                return store.Items[i];
            }
        }
        return null;
    }

    /**
     * Get a App Store
     * @param {string} id App Store Application ID
     * @return {object} App Store Application or null
     */
    this._getStore = function (id) {

        for (var i = 0 ; i < self.stores.length ; i++) {
            if (self.stores[i].ID == id) {
                return self.stores[i];
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

        this._getApplications(function (stores) {
            // Success
            // Update the current applications list with the new applications list
            self._updateStoreList(stores);

            if (typeof (successCallback) == "function") {
                successCallback();
            }
        }, function (response) {
            // Error
            self.stores.length = 0;

            if (typeof (errorCallback) == "function") {
                errorCallback(response);
            }
        });
    }

    /**
     * Update current local App Store applications list with new list
     * @param {array} newStores New AppStore application list
     */
    this._updateStoreList = function (newStores) {

        var newList = [];
        var removeList = [];


        // Check for new stores and update current store
        for (var i = 0; i < newStores.length; i++) {

            var newAppStore = newStores[i];
            var appStore = this._getStore(newAppStore.ID);
            if (appStore == null) {
                newList.push(newAppStore);
            } else {

                appStore.DisplayName = newAppStore.DisplayName;
                appStore.Updates = newAppStore.Updates;

                // Update Items
                this._updateApplicationsList(appStore, newAppStore.Items);
            }
        }

        // Remove removed stores from store  list
        for (var i = 0; i < self.stores.length; i++) {

            var appStore = self.stores[i];
            var bExists = false;
            // Check if it exist in newList
            for (var n = 0; n < newStores.length; n++) {
                var newAppStore = newStores[n];

                if (appStore.ID == newAppStore.ID) {
                    bExists = true;
                    break;
                }
            }

            if (bExists == false) {
                removeList.push(appStore);
            }
        }

        // Remove installed applications from installed applications list
        for (var i = 0; i < removeList.length; i++) {
            var index = self.stores.indexOf(removeList[i]);
            if (index > -1) {
                self.stores.splice(index, 1);
            }
            this._onRemovedStore(removeList[i]);
        }

        // Add new installed applications
        for (var i = 0; i < newList.length; i++) {
            self.stores.push(newList[i]);
            this._onNewStore(newList[i]);
        }
    }

    /**
     * Update current local App Store applications list with new list
     * @param {array} newApplications New AppStore application list
     */
    this._updateApplicationsList = function (store, newApplications) {

        var newList = [];
        var removeList = [];

        // Check for new installed applications and update current installed application
        for (var i = 0; i < newApplications.length; i++) {

            var newAppStoreApplication = newApplications[i];
            var appStoreApplication = this._getApplication(store, newAppStoreApplication.ID);
            if (appStoreApplication == null) {
                newList.push(newAppStoreApplication);
            } else {
                UtilsFactory.updateObject(newAppStoreApplication, appStoreApplication, function (arg) { });
            }
        }

        // Remove removed installed applications from installed application list
        for (var i = 0; i < store.Items.length; i++) {

            var appStoreApplication = store.Items[i];
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
            var index = store.Items.indexOf(removeList[i]);
            if (index > -1) {
                store.Items.splice(index, 1);
            }
            this._onRemovedApplication(removeList[i]);
        }

        // Add new installed applications
        for (var i = 0; i < newList.length; i++) {
            store.Items.push(newList[i]);
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
     * On New AppStore application Event
     * @param {object} store AppStore store
     */
    this._onNewStore = function (store) {
    }

    /**
     * On AppStore application Removed event
     * @param {object} store AppStore store
     */
    this._onRemovedStore = function (store) {
    }

    /**
     * Install AppStore application
     * @param {object} application App Store application
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.install = function (application, successCallback, errorCallback) {

        var uri = "/api/admin/installed/task";

        application.task = { "Text": "Installing" };

        // Clone application instance so we can remove unsupported properties
        // Make the posted data match the server expected data format
        var appData = JSON.parse(JSON.stringify(application));
        delete appData.task;

        var task = { "Type": "Install", "SourceUrl": application.SourceUrl };

        $http.post(uri, task).then(function (response) {

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

    /**
     * Upgrade AppStore application
     * @param {object} application App Store application
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.upgrade = function (application, successCallback, errorCallback) {

        var uri = "/api/admin/installed/task";

        application.task = { "Text": "Upgrading" };

        // Clone application instance so we can remove unsupported properties
        // Make the posted data match the server expected data format
        var appData = JSON.parse(JSON.stringify(application));
        delete appData.task;

        var task = { "Type": "Upgrade", "ID": application.ID };

        $http.post(uri, task).then(function (response) {

            // Success (202, 204)
            application.task = null;

            $log.info("App Store Application " + application.DisplayName + " was successfully upgraded");

            if (typeof (successCallback) == "function") {
                successCallback();
            }

            self.refreshApplications();

        }, function (response) {

            application.task = null;

            // Error
            var errorHeader = "Failed to upgrade App Store application";
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

                self.refreshApplications();

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

        var uri = "/api/admin/installed/task";

        var task = { "Type": "UnInstall", "ID": application.ID };

        $http.post(uri, task).then(function (response) {

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