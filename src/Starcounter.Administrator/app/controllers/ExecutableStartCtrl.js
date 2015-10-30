/**
 * ----------------------------------------------------------------------------
 * Start Executable page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('ExecutableStartCtrl', ['$scope', '$log', '$location', '$routeParams', 'HostModelService', 'NoticeFactory', 'UserMessageFactory', 'ApplicationService', function ($scope, $log, $location, $routeParams, HostModelService, NoticeFactory, UserMessageFactory, ApplicationService) {

    var self = this;

    $scope.database = null;

    // Entered or Selected file
    $scope.pickedApplications = null;
    $scope.selectedApplication = null;

    // List of recent successfully started applications
    $scope.recentApplications = [];
    $scope.notice = null;

    $scope.HasErrorMessage = false;
    $scope.ErrorMessage = {
        Message: "",
        HelpLink: ""
    }
    /**
     * Start Application
     */
    $scope.btnStart = function (application) {

        this.HasErrorMessage = false;

        //var application = {
        //    "Uri": "",
        //    "Path": $scope.file,
        //    "ApplicationFilePath": $scope.file,
        //    "Name": $scope.file.replace(/^.*[\\\/]/, ''),
        //    "Description": "",
        //    "Arguments": [{
        //        "dummy": $scope.file
        //    }],
        //    "DefaultUserPort": 0,
        //    "ResourceDirectories": [],
        //    "WorkingDirectory": null,
        //    "IsTool": true,
        //    "StartedBy": "Starcounter Administrator",
        //    "Engine": { "Uri": "" },
        //    "RuntimeInfo": {
        //        "LoadPath": "",
        //        "Started": "",
        //        "LastRestart": ""
        //    },
        //    "databaseName": $scope.selectedDatabaseName
        //};

        application.databaseName = $scope.database.ID;
        var self = this;
        ApplicationService.startLocalExecutable(application,
            function () {
                // Success

                // Remember successfully started applications
                $scope.rememberRecentApplication(application);

                // Navigate to Application list if user has not navigated to another page
                if ($location.path() == ("/databases/" + $scope.database.ID + "/executabeStart")) {
                    $location.path("/databases/" + $scope.database.ID);
                }

            },
            function (messageObject) {
                // Error

                self.HasErrorMessage = true;
                self.ErrorMessage.Message = messageObject.message;
                self.ErrorMessage.HelpLink = messageObject.helpLink;

                if (messageObject.isError) {
                    UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
                }
            });
    }

    /**
     * Create empty Application
     * @return {object} Application
     */
    $scope.createEmptyApplication = function () {

        var application = {
            "Uri": "",
            "Path": "",
            "ApplicationFilePath": "",
            "Name": "",
            "Description": "",
            "Arguments": [{
                "dummy": ""
            }],
            "DefaultUserPort": 0,
            "ResourceDirectories": [],
            "WorkingDirectory": null,
            "AsyncEntrypoint": false,
            "StartedBy": "Starcounter Administrator",
            "Engine": { "Uri": "" },
            "RuntimeInfo": {
                "LoadPath": "",
                "Started": "",
                "LastRestart": ""
            },
            "databaseName": ""
        };

        return application;
    }

    /**
     * Select Application
     * @param {object} application Application
     */
    $scope.btnSelect = function (application) {

        $scope.selectedApplication = angular.copy(application);
    }

    /**
     * Pick Application
     * @param {object} application Application
     */
    $scope.btnPick = function () {

        ApplicationService.pickExecutable(function (response) {

            $scope.pickedApplications = response;

            if ($scope.pickedApplications.length > 0) {
                // No support for multistarts at the moment
                $scope.selectedApplication.Path = $scope.pickedApplications[0].file;
            }

        }, function (messageObject) {

            if (messageObject.isError) {
                UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
            }
            else {
                $scope.notice = NoticeFactory.ShowNotice({ type: 'danger', msg: messageObject.message, helpLink: messageObject.helpLink });
            }
        });
    }

    /**
     * Remember Application
     * @param {object} application Application
     */
    $scope.rememberRecentApplication = function (application) {

        var maxItems = 5;
        // Check if file is already 'rememberd'
        for (var i = 0; i < $scope.recentApplications.length ; i++) {

            // Applicaion already rememberd
            if (application.Name == $scope.recentApplications[i].Name &&
                application.databaseName == $scope.recentApplications[i].databaseName) {
                return;
            }

        }

        // Add new items to the beginning of an array:
        $scope.recentApplications.unshift(application);

        var toMany = $scope.recentApplications.length - maxItems;

        if (toMany > 0) {
            $scope.recentApplications.splice(maxItems, toMany);
        }

        var str = JSON.stringify($scope.recentApplications);
        localStorage.setItem("recentApplications", str);
    }

    /**
     * Refresh recent remembered applications list
     */
    $scope.refreshRecentApplications = function () {

        if (typeof (Storage) !== "undefined") {
            var result = localStorage.getItem("recentApplications");
            if (result) {
                try {
                    $scope.recentApplications = JSON.parse(result);
                }
                catch (err) {
                    $log.error(err, "Removing invalid application history");
                    localStorage.removeItem("recentApplications");
                }
            }
        }
        else {
            // No web storage support..
        }
    }

    // Init
    // Set Data
    $scope.database = HostModelService.getDatabase($routeParams.name);

    $scope.selectedApplication = $scope.createEmptyApplication();

    $scope.$watch('selectedApplication.Path', function (newValue, oldValue) {
        $scope.selectedApplication.Path = newValue;
        $scope.selectedApplication.ApplicationFilePath = newValue;

        // TODO: Assure name is valid, no dots (.) etc..

        if (newValue) {
            $scope.selectedApplication.Name = newValue.replace(/^.*[\\\/]/, '');
        }
        else {
            $scope.selectedApplication.Name = "";
        }
        $scope.selectedApplication.Arguments["dummy"] = newValue;
    });

    $scope.refreshRecentApplications();
}]);