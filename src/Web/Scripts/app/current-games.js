/// <reference path="../knockout-3.4.0.js" />
/// <reference path="../moment.js" />
/// <reference path="common.js" />
/// <reference path="../amplify.min.js" />
 
ko.components.register('current-games', {
    viewModel: function () {
        var createBet = function (data) {
            var result = ko.mapping.fromJS(data);
            result.DetailData = ko.observableArray([]);
            
            result.OpenedAtFmt = ko.computed(function () {
                return common.formatDate(result.OpenedAt());
            });
            result.ClosedAtFmt = ko.computed(function () {
                return common.formatDate(result.ClosedAt());
            });
            result.DrawAtFmt = ko.computed(function () {
                return common.formatDate(result.DrawAt());
            });

            result.TotalAmountFmt = ko.computed(function() {
                return result.TotalAmount().toString();
            });

            return result;
        };
 
        var mapping = {
            key: function(item) {
                return ko.utils.unwrapObservable(item.OpenedAt);
            },
            create: function (options) {
                return createBet(options.data);
            }
        };

        var self = this;
        self.expandedGameNames = [];
        self.data = ko.mapping.fromJS([], mapping);

        self.dataToggle = function () {
            if (window.common.address !== undefined) {
                return 'collapse';
            } else return '';
        };

        var findEntry = function (name) {
            return ko.utils.arrayFirst(self.data(), function (item) {
                return name === item.Name();
            });
        };

        // SignalR
        var gameHub = $.connection.GameHub;
        gameHub.client.onBetPlaced = function (game) {
            if (self.data().length > 0) {
                var entry = findEntry(game.Name);
                if (entry !== null) {

                    entry.Bets(game.Bets);
                    entry.Status(game.Status);
                    entry.TotalAmount(game.TotalAmount);

                    if (window.common.address !== undefined && $.inArray(game.Name, self.expandedGameNames) > -1) {
                        $.ajax({
                            url: "/api/game/bets/" + encodeURIComponent(game.Name) + "/" + encodeURIComponent(window.common.address),
                            type: "GET",
                            contentType: "application/json; charset=utf-8",
                            dataType: "json"
                        }).done(function (data) {
                            entry.DetailData([]);
                            entry.DetailData(data);
                        });
                    }
                }
            }
        };
        gameHub.client.onCurrentGamesUpdate = function (result) {
            common.show(true, 'current-games');
            ko.mapping.fromJS([], mapping, self.data);
            ko.mapping.fromJS(result, mapping, self.data);
            common.show(false, 'current-games');
        };


        //Initial load
        $.ajax({
            url: "/api/game/current",
            type: "GET",
            contentType: "application/json; charset=utf-8",
            dataType: "json"
        }).done(function (data) {
            ko.mapping.fromJS(data, mapping, self.data);
            common.show(false, 'current-games');

            // UI EVENTS
            var table = $('#current-games-tbl');
            $(table).on('hide.bs.collapse', function (e) {
                var placeholderId = '#' + e.target.id;
                var gameName = $(placeholderId).data("name");
                amplify.publish("collapse-current-game", { gameName: gameName });
            });
            amplify.subscribe("collapse-current-game", function (p) {
                if (self.data().length > 0) {
                    var entry = ko.utils.arrayFirst(self.data(), function (item) {
                        return p.gameName === item.Name();
                    });
                    if (entry !== null) {
                        entry.DetailData([]);
                        self.expandedGameNames.splice(self.expandedGameNames.indexOf(p.gameName), 1);
                    }
                }
            });
            $(table).on('show.bs.collapse', function (e) {
                var placeholderId = '#' + e.target.id;
                var gameName = $(placeholderId).data("name");
                amplify.publish("expand-current-game", { gameName: gameName });
            });
            amplify.subscribe("expand-current-game", function (p) {
                if (self.data().length > 0) {
                    var entry = findEntry(p.gameName);
                    if (entry !== null) {
                        $.ajax({
                            url: "/api/game/bets/" + encodeURIComponent(p.gameName) + "/" + encodeURIComponent(window.common.address),
                            type: "GET",
                            contentType: "application/json; charset=utf-8",
                            dataType: "json"
                        }).done(function (data) {
                            common.show(true, 'current-games-details-' + p.gameName);
                            entry.DetailData([]);
                            entry.DetailData(data);
                            self.expandedGameNames.push(p.gameName);
                            common.show(false, 'current-games-details-' + p.gameName);
                        });
                    }
                }
            });
        });
         
    },
    template: { element: 'current-games-tmpl' }
});
