/**
 * ----------------------------------------------------------------------------
 * Create Databases page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('DatabaseCreateCtrl', ['$scope', '$log', '$location', '$anchorScroll', 'NoticeFactory', 'ServerService', 'DatabaseService', 'UserMessageFactory', function ($scope, $log, $location, $anchorScroll, NoticeFactory, ServerService, DatabaseService, UserMessageFactory) {

    // Database Default settings
    $scope.settings = null;

    // List of available collations
    $scope.collations = null;

    $scope.orginalSettings = null;

    $scope.modified = {
        ImageDirectory: false,
        TempDirectory: false,
        TransactionLogDirectory: false,
        DumpDirectory: false
    }

    /**
     * Create database
     * @param {object} settings Settings
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    $scope.createDatabase = function (settings, successCallback, errorCallback) {

        DatabaseService.createDatabase(settings, function () {
            // Success

            // TODO: Return the newly created database
            if (typeof (successCallback) == "function") {
                successCallback();
            }

        }, function (messageObject) {
            // Error
            if (typeof (errorCallback) == "function") {
                errorCallback(messageObject);
            }
        });

    }


    /**
     * Refresh database default settings
     * @param {object} settings Settings
     */
    $scope.btnCreateDatabase = function (settings) {

        // Scroll top top
        $location.hash('top');
        $anchorScroll();


        DatabaseService.createDatabase(settings, function (database) {

            // NOTE: The incoming database does only contain one propety 'name'

            // Success
            NoticeFactory.ShowNotice({ type: 'success', msg: "The Database " + database.name + " was successfully created" });

            // Navigate to database list if user has not navigated to another page
            if ($location.path() == "/databaseCreate") {
                $location.path("/databases");
            }


        }, function (messageObject, validationErrors) {

            // Error

            if (validationErrors != null && validationErrors.length > 0) {
                // Validation errors
                var scrollToFirstError = false;
                // Show errors on screen
                for (var i = 0; i < validationErrors.length; i++) {
                    //$scope.alerts.push({ type: 'danger', msg: validationErrors[i].message });
                    if ($scope.myForm[validationErrors[i].PropertyName] == undefined) {
                        NoticeFactory.ShowNotice({ type: 'danger', msg: "Missing or invalid property: " + validationErrors[i].PropertyName });
                    } else {

                        $scope.myForm[validationErrors[i].PropertyName].$setValidity("validationError", false);
                        var id = validationErrors[i].PropertyName;
                        var unregister = $scope.$watch("settings." + validationErrors[i].PropertyName, function (newValue, oldValue) {
                            if (newValue == oldValue) return;
                            $scope.myForm[id].$setValidity("validationError", true);
                            unregister();
                        }, false);

                        if (scrollToFirstError == false) {
                            scrollToFirstError = true;

                            // Scroll to invalid value
                            $('html, body').animate({
                                scrollTop: $("#" + id).offset().top
                            }, 1000);
                        }
                    }

                }

                $scope.step1open = true;

            }
            else {


                if (messageObject.isError) {

                    //var message = messageObject.message.replace(/\r\n/g, "<br>");

                    UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
                }
                else {
                    NoticeFactory.ShowNotice({ type: 'danger', msg: messageObject.message, helpLink: messageObject.helpLink });
                }
            }

            if (typeof (errorCallback) == "function") {
                errorCallback(messageObject);
            }
        });

    }


    /**
     * Reset database settings
     */
    $scope.btnResetSettings = function () {
        $scope.refreshDatabaseDefaultSettings();
    }


    /**
     * Refresh database default settings
     */
    $scope.refreshDatabaseDefaultSettings = function () {

        DatabaseService.getDatabaseDefaultSettings(function (settings) {
            // Success

            $scope.orginalSettings = angular.copy(settings);
            $scope.settings = settings;

            $scope.setFolderNames(settings.Name);

            // Clear modified flag
            $scope.clearFolderModifiedFlag();

            ServerService.getCollations(function (collations) {
                // Success
                $scope.collations = collations;
            }, function (messageObject) {
                // Error
                UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);

            });

            $scope.myForm.$setPristine(); // This dosent work, the <select> breaks the pristine state :-(

        },
            function (messageObject) {
                // Error

                if (messageObject.isError) {
                    UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
                }
                else {
                    NoticeFactory.ShowNotice({ type: 'danger', msg: messageObject.message, helpLink: messageObject.helpLink });
                }

            });
    }


    /**
     * Event when the user changed the folder
     * @param {string} propertyName Property name of the folder
     */
    $scope.onUserChangedFolder = function (propertyName) {
        $scope.modified[propertyName] = true;
    }


    /**
     * Set folder names based on database name
     * Ignore already changed foldernames
     * @param {string} databaseName Database Name
     */
    $scope.setFolderNames = function (databaseName) {

        if (!$scope.modified.ImageDirectory) {
            $scope.settings.ImageDirectory = $scope.orginalSettings.ImageDirectory.replace("[DatabaseName]", databaseName);
        }
        if (!$scope.modified.TempDirectory) {
            $scope.settings.TempDirectory = $scope.orginalSettings.TempDirectory.replace("[DatabaseName]", databaseName);
        }
        if (!$scope.modified.TransactionLogDirectory) {
            $scope.settings.TransactionLogDirectory = $scope.orginalSettings.TransactionLogDirectory.replace("[DatabaseName]", databaseName);
        }
        if (!$scope.modified.DumpDirectory) {
            $scope.settings.DumpDirectory = $scope.orginalSettings.DumpDirectory.replace("[DatabaseName]", databaseName);
        }

    }


    /**
     * Clear the folders modified flag
     */
    $scope.clearFolderModifiedFlag = function () {

        $scope.modified.ImageDirectory = false;
        $scope.modified.TempDirectory = false;
        $scope.modified.TransactionLogDirectory = false;
        $scope.modified.DumpDirectory = false;
    }


    /**
     * Wache the database name and update unmodifed folder paths
     */
    $scope.$watch("settings.Name", function (newValue, oldValue) {
        if (newValue !== oldValue) {
            $scope.setFolderNames(newValue);
        }
    });


    // Init
    $scope.refreshDatabaseDefaultSettings();

}]);