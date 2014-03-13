/**
 * User ErrorMessage controller
 * https://github.com/angular-ui/bootstrap/issues/1798
 */
adminModule.controller('UserErrorMessageCtrl', function ($scope, $timeout, $modalInstance, model) {

    $scope.model = model;

    $scope.close = function (result) {

        $modalInstance.close(result);
          
    };

    $scope.btnClick = function (button) {

        $modalInstance.close(button.result);

    }

});