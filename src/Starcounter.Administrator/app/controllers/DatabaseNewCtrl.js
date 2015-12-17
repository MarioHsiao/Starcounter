/**
 * ----------------------------------------------------------------------------
 * Create Databases page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('DatabaseNewCtrl', ['$scope', '$log', '$location', 'HostModelService', '$anchorScroll', 'NoticeFactory', 'ServerService', 'DatabaseService', 'UserMessageFactory', function ($scope, $log, $location, HostModelService, $anchorScroll, NoticeFactory, ServerService, DatabaseService, UserMessageFactory) {

    $scope.data = HostModelService.data;

    $scope.Status = 0;
    $scope.HasErrorMessage = false;
    $scope.ErrorMessage = {
        Message: "",
        HelpLink: ""
    };

    $scope.SuccessMessage = null;

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
     */
    $scope.btnCreateDatabase = function (settings) {

        $scope.Status = 1;
        $scope.StatusText = "Creating new database...";
        $scope.HasErrorMessage = false;
        $scope.SuccessMessage = null;

        // Scroll top top
//        $location.hash('top');
//        $anchorScroll();

        DatabaseService.createDatabase(settings, function (databaseName) {

            // NOTE: The incoming database does only contain one propety 'name'
            $scope.StatusText = "Starting database..."
            // Start database engine
            DatabaseService.startEngine(databaseName, function(){
                // Success

                $scope.SuccessMessage = "Created and started.";
                // Success
                // Navigate to database list if user has not navigated to another page
                if ($location.path() == "/databaseNew") {
                    $location.path("/databases/" + databaseName);
                }
                $scope.Status = 0;
                $scope.StatusText = "";
            }, function (messageObject) {

                // Error
                $scope.Status = 0;
                $scope.StatusText = "";
                $scope.SuccessMessage = "Created but failed to start.";

                $scope.HasErrorMessage = true;
                $scope.ErrorMessage.Message = messageObject.message;
                $scope.ErrorMessage.HelpLink = messageObject.helpLink;

                if (messageObject.isError) {
                    UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
                }
  
            });

        }, function (messageObject, validationErrors) {

            $scope.Status = 0;
            $scope.StatusText = "";

            // Error

            if (validationErrors != null && validationErrors.length > 0) {
                // Validation errors
                var scrollToFirstError = false;
                // Show errors on screen
                for (var i = 0; i < validationErrors.length; i++) {
                    //$scope.alerts.push({ type: 'danger', msg: validationErrors[i].message });
                    if ($scope.myForm[validationErrors[i].PropertyName] == undefined) {
                        //NoticeFactory.ShowNotice({ type: 'danger', msg: "Missing or invalid property: " + validationErrors[i].PropertyName });

                        $scope.HasErrorMessage = true;
                        $scope.ErrorMessage.Message = "Missing or invalid property: " + validationErrors[i].PropertyName;
                        $scope.ErrorMessage.HelpLink = null;


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

                $scope.HasErrorMessage = true;
                $scope.ErrorMessage.Message = messageObject.message;
                $scope.ErrorMessage.HelpLink = messageObject.helpLink;

                if (messageObject.isError) {
                    UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
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

        $scope.HasErrorMessage = false;
        $scope.SuccessMessage = null;

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
                    $scope.HasErrorMessage = true;
                    $scope.ErrorMessage.Message = messageObject.message;
                    $scope.ErrorMessage.HelpLink = messageObject.helpLink;

//                    NoticeFactory.ShowNotice({ type: 'danger', msg: messageObject.message, helpLink: messageObject.helpLink });
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