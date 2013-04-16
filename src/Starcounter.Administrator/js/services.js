
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
            query: { method: 'GET', isArray: false },       //
            save: { method: 'PUT', isArray: false }         // Save
        });
    });


    $provide.factory('DbWorkaround', function ($resource) {
        return $resource('/a/:name', { name: '@name' }, {
            start: { method: 'POST', params: { action: 'start' }, isArray: false },
            stop: { method: 'POST', params: { action: 'stop' }, isArray: false },
        });
    });

    $provide.factory('Database', function ($resource) {
        return $resource('/databases/:name', { name: '@name' }, {
            create: { method: 'POST', isArray: false },     // Create new database
            start: { method: 'POST', params: { action: 'start' }, isArray: false },
            stop: { method: 'POST', params: { action: 'stop' }, isArray: false },
            query: { method: 'GET', isArray: false },       // Get all databases
            get: { method: 'GET', isArray: false },         // Get one database
            save: { method: 'PUT', isArray: false },        // Save
        });
    });

    //$provide.factory('Database', function ($resource) {

    //    return $resource('/databases/:databaseId', { databaseId: '@databaseId' }, {
    //        query: { method: 'GET', isArray: false },
    //        save: { method: 'POST', isArray: false }
    //        //get: { method: 'GET', headers: { 'Content-Type': 'application/json' } }
    //    });

    //});

    $provide.factory('App', function ($resource) {

        return $resource('/apps/:appID', { appID: '@appID' }, {
            query: { method: 'GET', isArray: false },
        });

    });


    $provide.factory('Log', function ($resource) {

        return $resource('/log', {}, {
            query: { method: 'GET', isArray: false }
        });

    });

    $provide.factory('Sql', function ($resource) {

        return $resource('/sql', {}, {
            query: { method: 'GET', isArray: false }
        });

    });

    $provide.factory('SqlQuery', function ($resource) {

        return $resource('/sql/:name', { name: '@name' }, {
            send: { method: 'POST', isArray: false }    // We need to override this (the return type is not an array)
        });

        //        return $resource('/__:databaseName/sql', { databaseName: '@databaseName' }, {
        //            send: { method: 'POST', isArray: false }    // We need to override this (the return type is not an array)
        //        });

    });

    $provide.factory('Console', function ($resource) {
        return $resource('/databases/:name/console', { name: '@name' }, {
            query: { method: 'GET', isArray: false }
        });
    });

    $provide.factory('CreateDatabase', function ($resource) {
        return $resource('/databases/:databaseName', { databaseName: '@databaseName' }, {
            save: { method: 'POST', isArray: false }
        });
    });

    $provide.factory('Settings', function ($resource) {
        return $resource('/settings/default/:type', { type: '@type' }, {
            query: { method: 'GET', isArray: false }
        });
    });

    $provide.factory('CommandStatus', function ($resource) {
        return $resource('/command/:commandId', { commandId: '@commandId' }, {
            query: { method: 'GET', isArray: false }
        });
    });



});

