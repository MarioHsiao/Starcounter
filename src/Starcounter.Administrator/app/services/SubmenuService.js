/**
 * ----------------------------------------------------------------------------
 * Applications Service
 * ----------------------------------------------------------------------------
 */
adminModule.service('SubmenuService', ['$http', '$rootScope', '$log', '$sce', 'UtilsFactory', 'JobFactory', function ($http, $rootScope, $log, $sce, UtilsFactory, JobFactory) {

    var self = this;

    this.model = {
        "menues": [],
        "isDatabase": false
    }

}]);