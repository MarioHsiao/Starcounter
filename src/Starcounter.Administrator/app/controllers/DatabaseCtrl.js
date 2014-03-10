/**
 * ----------------------------------------------------------------------------
 * Database page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('DatabaseCtrl', ['$scope', '$log', '$routeParams', 'NoticeFactory', 'HostModelService', 'DatabaseService', 'UserMessageFactory', function ($scope, $log, $routeParams, NoticeFactory, HostModelService, DatabaseService, UserMessageFactory) {

    $scope.model = {
        database: null
    }


    /**
     * Get Console output
     * @param {object} database Database
     */
    $scope.btnGetConsoleOutput = function (database) {

        DatabaseService.refreshConsoleOuput(database, function () {

             $("#console").scrollTop($("#console")[0].scrollHeight);

            // Success
        }, function (messageObject) {
            // Error
            UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);

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
        // TODO: $digest already in progress
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

    $scope.calcWidth = function () {
        var border = 12;
        var leftOffset = $("#console").offset().left;
        var width = $scope.winWidth - leftOffset - 2 * border;
        if (width < 150) {
            return 150;
        }
        return width;
    };

}]);