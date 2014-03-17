/**
 * ----------------------------------------------------------------------------
 * Database page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('DatabaseCtrl', ['$scope', '$log', '$routeParams', '$location', '$sce', 'NoticeFactory', 'HostModelService', 'DatabaseService', 'UserMessageFactory', function ($scope, $log, $routeParams, $location, $sce, NoticeFactory, HostModelService, DatabaseService, UserMessageFactory) {

    $scope.model = {
        database: null
    }


    /**
     * Get Console output
     * @param {object} database Database
     */
    $scope.btnGetConsoleOutput = function (database) {

        DatabaseService.refreshConsoleOuput(database, function () {

            // Success
        }, function (messageObject) {
            // Error
            UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);

        });

    }


    /**
     * Delete Database
     * @param {object} database Database
     */
    $scope.btnDeleteDatabase = function (database) {

        var title = "Delete database";

        var message = $sce.trustAsHtml("This will delete the database <strong>" + database.name + "</strong> permantely.</br>All data will be completly deleted, with no ways to recover it.</br>This action is not possible to reverse.");
        var buttons = [{ result: 0, label: 'Delete Database', cssClass: 'btn-danger' }, { result: 1, label: 'Cancel', cssClass: 'btn' }];
        var model = { "title": title, "message": message, "buttons": buttons, enteredDatabaseName:"" };
        model.pattern = "/^" + database.name + "$/";
        UserMessageFactory.showModal('app/partials/databaseDeleteModal.html', 'UserErrorMessageCtrl', model, function (result) {

            if (result == 0) {

                DatabaseService.deleteDatabase(database, function () {
                    // Success
                    NoticeFactory.ShowNotice({ type: 'success', msg: "The database " + database.name + " was deleted", helpLink: null });

                    // Navigate to database list if user has not navigated to another page
                    if ($location.path() == "/databases/" + database.name) {
                        $location.path("/databases");
                    }


                }, function (messageObject) {

                    // Error
                    if (messageObject.isError) {
                        UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
                    }
                    else {
                        NoticeFactory.ShowNotice({ type: 'danger', msg: messageObject.message, helpLink: messageObject.helpLink });
                    }

                });
            }
        });

    }



    // Init
    // Refresh host model
    HostModelService.refreshHostModel(function () {

        $scope.model.database = DatabaseService.getDatabase($routeParams.name);

    }, function (messageObject) {
        // Error
        UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
    });


    // Console fixe the height.
    var $window = $(window);
    $scope.winHeight = $window.height();
    $scope.winWidth = $window.width();
    $window.resize(function () {
        $scope.winHeight = $window.height();
        $scope.winWidth = $window.width();
        $scope.$apply();
    });

    $scope.calcHeight = function () {
        var border = 12;
        var ht = $("#console");
        var offset = ht.offset();
        if (!offset) {
            return;
        }
        var topOffset = offset.top;

        var height = $scope.winHeight - topOffset - 2 * border;
        if (height < 150) {
            return 150;
        }
        return height;
    };

    $scope.sizeStyle = function () {
        return { "height": $scope.calcHeight() + "px", "background-color": "#ff0000" };
    }


}]);