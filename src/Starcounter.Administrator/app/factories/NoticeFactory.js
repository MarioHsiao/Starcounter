/**
 * ----------------------------------------------------------------------------
 * Notice Factory
 * ----------------------------------------------------------------------------
 */
adminModule.service('NoticeFactory', ['$log', function ($log) {

    var factory = {};

    // object { type: "error", msg: "message", helpLink: "helpLink" }
    factory.notises = [];

    //factory.notises.push({ type: "error", msg: "message", helpLink: "helpLink" });

    /**
     * Add notice
     * @param {object} notice Notice
     * @return {object} Notice
     */
    factory.ShowNotice = function (notice) {
        factory.notises.push(notice);
        return notice;
    }


    /**
     * Clear all
     */
    factory.ClearAll = function () {
        factory.notises.length = 0;
    }


    /**
     * Close notice
     * @param {object} notice Notice
     */
    factory.CloseNotice = function (notice) {
        var index = factory.notises.indexOf(notice);
        if (index > -1) {
            factory.notises.splice(index, 1);
        }
    }

    return factory;

}]);
