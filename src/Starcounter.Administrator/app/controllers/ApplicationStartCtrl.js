/**
 * ----------------------------------------------------------------------------
 * Start Application page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('ApplicationStartCtrl', ['$scope', '$log', '$location', 'NoticeFactory', 'UserMessageFactory', 'DatabaseService', 'ApplicationService', function ($scope, $log, $location, NoticeFactory, UserMessageFactory, DatabaseService, ApplicationService) {


    // List of databases
    $scope.databases = DatabaseService.databases;

    // Entered or Selected file
    //$scope.file = "";
    $scope.pickedApplications = null;

    $scope.selectedApplication = null;

    // Selected databasename
    //$scope.selectedDatabaseName = null;

    // List of recent successfully started applications
    $scope.recentApplications = [];

    $scope.notice = null;

    /**
     * Start Application
     */
    $scope.btnStart = function (application) {


        if ($scope.notice) {
            NoticeFactory.CloseNotice($scope.notice);
        }

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
        ApplicationService.startApplication(application,
            function () {
                // Success

                // Remember successfully started applications
                $scope.rememberRecentApplication(application);

                // Navigate to Application list if user has not navigated to another page
                if ($location.path() == "/applicationStart") {
                    $location.path("/applications");
                }

            },
            function (messageObject) {
                // Error

                if (messageObject.isError) {
                    UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
                }
                else {
                    $scope.notice = NoticeFactory.ShowNotice({ type: 'danger', msg: messageObject.message, helpLink: messageObject.helpLink });
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
            "IsTool": true,
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
        ApplicationService.pickApplications(function (response) {

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


    //$scope.$watch('selectedDatabaseName', function (newValue, oldValue) {
    //    $scope.selectedApplication.databaseName = newValue;
    //});



    DatabaseService.refreshDatabases(function () {

        if ($scope.databases.length > 0) {
            $scope.selectedApplication.databaseName = $scope.databases[0].name;
        }

    }, function (messageObject) {
        // Error
        UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
    });


    $scope.refreshRecentApplications();


}]);