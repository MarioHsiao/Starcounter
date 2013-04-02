
angular.module('scadminServices', ['ngResource'], function ($provide) {

    $provide.service('patchService', function ($resource) {

        this.applyPatch = function (source, location, patches) {
            var patchstring = JSON.stringify(patches);

            // Send json patch(es)
            // TODO: The Port is hardcoded
            var resource = $resource('http://localhost\\:80' + location, {}, { send: { method: 'PATCH', isArray: true } });

            // Send the json-patch data
            resource.send(patchstring, function (data) {
                console.log("REQUEST (" + location + ") PATCH: " + patchstring);

                console.log("RESPONSE (" + location + ") PATCH:" + JSON.stringify(data));
                jsonpatch.apply(source, data);
            });

        }
    });

    $provide.factory('Server', function ($resource) {

        return $resource('/server', {}, {
            query: { method: 'GET', isArray: false }    // We need to override this (the return type is not an array)
        });

    });

    $provide.factory('Database', function ($resource) {

        return $resource('/databases/:databaseId', { databaseId: '@databaseId' }, {
            query: { method: 'GET', isArray: false }   // We need to override this (the return type is not an array)
            //get: { method: 'GET', headers: { 'Content-Type': 'application/json' } }
        });

    });

    $provide.factory('Log', function ($resource) {

        return $resource('/log', {}, {
            query: { method: 'GET', isArray: false }    // We need to override this (the return type is not an array)
        });

    });

    $provide.factory('Sql', function ($resource) {

        return $resource('/sql', {}, {
            query: { method: 'GET', isArray: false }    // We need to override this (the return type is not an array)
        });

    });

    $provide.factory('SqlQuery', function ($resource) {

        return $resource('/sql/:databaseName', { databaseName: '@databaseName' }, {
            send: { method: 'POST', isArray: false }    // We need to override this (the return type is not an array)
        });

        //        return $resource('/__:databaseName/sql', { databaseName: '@databaseName' }, {
        //            send: { method: 'POST', isArray: false }    // We need to override this (the return type is not an array)
        //        });

    });

    $provide.factory('Console', function ($resource) {
        return $resource('/databases/:databaseName/console', { databaseName: '@databaseName' }, {
            query: { method: 'GET', isArray: false }    // We need to override this (the return type is not an array)
        });
    });


 });

