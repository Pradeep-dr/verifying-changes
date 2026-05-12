// Mirrors the structure of real Mars's `waitingLists.app.js` —
// an AngularJS module wired up with ngRoute and HTML5 mode.
angular.module('WaitingListsApp', ['ngRoute'])

    .config(['$routeProvider', '$locationProvider', function ($routeProvider, $locationProvider) {
        $routeProvider
            .when('/',             { templateUrl: '/Scripts/app/waitingLists/views/overview.html',     controller: 'OverviewController',     controllerAs: 'vm' })
            .when('/config',       { templateUrl: '/Scripts/app/waitingLists/views/config.html',       controller: 'ConfigController',       controllerAs: 'vm' })
            .when('/entries',      { templateUrl: '/Scripts/app/waitingLists/views/entries.html',      controller: 'EntriesController',      controllerAs: 'vm' })
            .when('/patientlists', { templateUrl: '/Scripts/app/waitingLists/views/patientlists.html', controller: 'PatientListsController', controllerAs: 'vm' })
            .otherwise({ redirectTo: '/' });

        $locationProvider.html5Mode(true);
    }]);
