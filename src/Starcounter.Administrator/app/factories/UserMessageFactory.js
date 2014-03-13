﻿/**
 * User Message Factory
 * Show messages to user (alert's, modal windows, etc..)
 */
adminModule.factory('UserMessageFactory', ['$modal', '$log', 'NoticeFactory', function ($modal, $log, NoticeFactory) {
    var factory = {};

    /**
     * Show Error message modal popup
     * TODO: Use showModal instead of this
     * @param {string} title Title
     * @param {string} message Message
     * @param {string} helpLink HelpLink Url
     * @param {string} stackTrace stackTrace
     */
    factory.showErrorMessage = function (title, message, helpLink, stackTrace) {

        var buttons = [{ result: 0, label: 'Close', cssClass: 'btn-primary' }];

        var modalInstance = $modal.open({
            templateUrl: 'app/partials/errorMessage.html',
            controller: 'UserErrorMessageCtrl',
            resolve: {
                model: function () {
                    return { "title": title, "message": message, "helpLink": helpLink, "stackTrace": stackTrace, "buttons": buttons };
                }
            }
        });

        modalInstance.result.then(function (result) {

            if (typeof (responseCallback) == "function") {
                responseCallback(result);
            }

        }, function () {
            NoticeFactory.ShowNotice({ type: 'error', msg: "The server is not responding or is not reachable.", helpLink: null });
            //            $log.info('Modal dismissed at: ' + new Date());
        });

    };


    /**
     * Show Message box
     * TODO: Use showModal instead of this
     * @param {string} title Title
     * @param {string} message Message
     * @param {object} buttons Buttons, Example: [{ result: 0, label: 'Ok', cssClass: 'btn' }, { result: 1, label: 'Cancel', cssClass: 'btn-danger' }]
     * @param {function} responseCallback Response Callback function
     */
    factory.showMessageBox = function (title, message, buttons, responseCallback) {

        var modalInstance = $modal.open({
            templateUrl: 'app/partials/modal.html',
            controller: 'UserErrorMessageCtrl',
            resolve: {
                model: function () {
                    return { "title": title, "message": message, "buttons": buttons };
                }
            }
        });

        modalInstance.result.then(function (result) {

            if (typeof (responseCallback) == "function") {
                responseCallback(result);
            }

        }, function () {
            NoticeFactory.ShowNotice({ type: 'error', msg: "The server is not responding or is not reachable.", helpLink: null });

            //            $log.info('Modal dismissed at: ' + new Date());
        });

    }


    /**
      * Show Modal Window
      * @param {string} templateUrl Template url
      * @param {string} controllerName the controller name
      * @param {model} model Data model
      * @param {function} responseCallback Response Callback function
      */
    factory.showModal = function (templateUrl, controllerName, model, responseCallback) {

        var modalInstance = $modal.open({
            templateUrl: templateUrl,
            controller: controllerName,
            resolve: {
                model: function () {
                    return model;
                }
            }
        });


        modalInstance.result.then(function (result) {

            if (typeof (responseCallback) == "function") {
                responseCallback(result);
            }

        }, function (response) {

            if (response !== undefined) {
                NoticeFactory.ShowNotice({ type: 'error', msg: "The server is not responding or is not reachable.", helpLink: null });
            }

            responseCallback();

        });

    }

    return factory;

}]);
