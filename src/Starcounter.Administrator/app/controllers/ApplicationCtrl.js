/**
 * ----------------------------------------------------------------------------
 * Application page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('ApplicationCtrl', ['$scope', '$log', '$sce', '$routeParams', 'UserMessageFactory', 'NoticeFactory', 'HostModelService', 'ApplicationService', function ($scope, $log, $sce, $routeParams, UserMessageFactory, NoticeFactory, HostModelService, ApplicationService) {

    $scope.model = {
        application: null
    }


    /**
     * Get Console output
     * @param {object} application aplication
     */
    $scope.btnGetConsoleOutput = function (application) {

        ApplicationService.refreshConsoleOuput(application, function () {

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

        $scope.model.application = HostModelService.getApplication($routeParams.name);

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
        //var topOffset = $("#console").offset().top;
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