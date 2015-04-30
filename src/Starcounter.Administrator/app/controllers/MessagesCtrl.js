/**
 * ----------------------------------------------------------------------------
 * Messages page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('MessagesCtrl', ['$scope', '$log', 'NoticeFactory', 'MessageService', 'DatabaseService', 'UserMessageFactory', function ($scope, $log, NoticeFactory, MessageService, DatabaseService, UserMessageFactory) {

    // List of databases
    $scope.messages = MessageService.messages;

    /**
     * Clear Message
     * @param {object} message Message
     */
    $scope.btnClearMessage = function (message) {

        MessageService.deleteMessage(message, function () { },
            function (messageObject) {
                // Error
                if (messageObject.isError) {
                    UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
                }
                else {
                    NoticeFactory.ShowNotice({ type: 'danger', msg: messageObject.message, helpLink: messageObject.helpLink });
                }

            });
    }

    /**
     * Clear All Message
     */
    $scope.btnClearAllMessages = function () {

        MessageService.deleteAllMessages( function () { },
                 function (messageObject) {
                     // Error
                     if (messageObject.isError) {
                         UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
                     }
                     else {
                         NoticeFactory.ShowNotice({ type: 'danger', msg: messageObject.message, helpLink: messageObject.helpLink });
                     }

                 });
    }


    // Init
    MessageService.refreshMessages(function () {


    }, function (messageObject) {
        // Error
        UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
    });

}]);