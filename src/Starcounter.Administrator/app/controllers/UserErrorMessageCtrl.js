/**
 * User ErrorMessage controller
 */
adminModule.controller('UserErrorMessageCtrl', function ($scope, $modalInstance, model) {

    $scope.model = model;
    //$scope.title = model.title;
    //$scope.message = message;
    //$scope.buttons = buttons;

    $scope.close = function (result) {
        $modalInstance.close(result);
    };

    $scope.btnClick = function (button) {
        $modalInstance.close(button.result);
    }


    //$scope.model = dialog.options.data;

    /**
     * Close dialog
     * @param {object} result Result
     */
    //$scope.close = function (result) {
    //    //  dialog.close(result);
    //};

});