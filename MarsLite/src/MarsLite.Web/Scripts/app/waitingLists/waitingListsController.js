angular
  .module("WaitingListsApp")

  .controller("WaitingListsController", [
    "$scope",
    "wlBootstrap",
    "$location",
    function ($scope, wlBootstrap, $location) {
      // Tell Angular's $locationProvider where the app root is so its routes
      // are interpreted relative to /waitinglists/{providerId}/.
      // In real Mars this is set via a <base href="..." /> tag in the layout.
      $scope.init = function (providerId, angularBase) {
        $scope.providerId = providerId;
        $scope.angularBase = angularBase;
      };
    },
  ])

  .controller("OverviewController", [
    "$http",
    "wlBootstrap",
    function ($http, boot) {
      var vm = this;
      vm.loading = true;
      // Nancy's default JSON serializer emits camelCase property names — match that.
      $http
        .get("/waitinglists/" + boot.providerId + "/data/lists")
        .then(function (r) {
          vm.lists = r.data || [];
          vm.active = vm.lists.filter(function (l) {
            return l.status === "Active";
          }).length;
          vm.total = vm.lists.reduce(function (n, l) {
            return n + l.entryCount;
          }, 0);
          vm.loading = false;
        });
    },
  ])

  .controller("ConfigController", [
    "$http",
    "wlBootstrap",
    function ($http, boot) {
      var vm = this;
      vm.loading = true;
      $http
        .get("/waitinglists/" + boot.providerId + "/data/config")
        .then(function (r) {
          vm.config = r.data || {};
          vm.loading = false;
        });
    },
  ])

  .controller("EntriesController", [
    "$http",
    "wlBootstrap",
    function ($http, boot) {
      var vm = this;
      vm.loading = true;
      $http
        .get("/waitinglists/" + boot.providerId + "/data/entries")
        .then(function (r) {
          var today = new Date(r.data.today);
          vm.entries = (r.data.entries || []).map(function (e) {
            var added = new Date(e.addedOn);
            var days = Math.floor((today - added) / 86400000);
            return Object.assign({}, e, {
              days: days,
              overdue: days > 40,
              dobLabel: new Date(e.patientDob).toLocaleDateString("en-GB", {
                day: "2-digit",
                month: "short",
                year: "numeric",
              }),
              addedLabel: added.toLocaleDateString("en-GB", {
                day: "2-digit",
                month: "short",
                year: "numeric",
              }),
            });
          });
          vm.loading = false;
        });
    },
  ])

  .controller("PatientListsController", [
    function () {
      var vm = this;
      vm.groups = [
        { name: "High priority", count: 12 },
        { name: "Needs review", count: 7 },
        { name: "Awaiting confirmation", count: 5 },
      ];
    },
  ]);
