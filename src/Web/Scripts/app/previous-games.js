/// <reference path="../knockout-3.3.0.js" />
/// <reference path="../moment.js" />
/// <reference path="common.js" />
/// <reference path="../amplify.min.js" />

ko.components.register('previous-games', {
    viewModel: function () {
        var createGame = function (data) {

            var result = ko.mapping.fromJS(data);
            result.DetailData = ko.observableArray([]);

            result.DrawAtFmt = ko.computed(function () {
                return common.formatDate(result.DrawedAt());
            });

            result.HasWinners = ko.computed(function () {
                var r = result.Status() === 'Drawed' && result.Winners() > 0;
                return r;
            });

            result.StatusFmt = ko.computed(function () {
                if (result.Status() === 'Drawed') {
                    return 'Selected';
                }

                return result.Status();
            });

            result.WinFmt = ko.computed(function () {
                return result.Win().toString();
            });

            result.TotalWinFmt = ko.computed(function () {
                return result.TotalWin().toString();
            });

            return result;
        };

        var mapping = {
            key: function (item) {
                return ko.utils.unwrapObservable(item.Name);
            },
            create: function (options) {
                return createGame(options.data);
            }
        };

        self.data = ko.mapping.fromJS([], mapping);

        // SignalR
        var gameHub = $.connection.GameHub;
        gameHub.client.onPreviousGamesUpdate = function (result) {
            common.show(true, 'previous-games-' + result.name);

            ko.mapping.fromJS([], mapping, self.data);
            ko.mapping.fromJS(result, mapping, self.data);

            common.show(false, 'previous-games-' + result.name);
        };

        //Initial load
        $.ajax({
            url: "/api/game/previous",
            type: "GET",
            contentType: "application/json; charset=utf-8",
            dataType: "json"
        }).done(function (data) {
            ko.mapping.fromJS(data, mapping, self.data);
            common.show(false, 'previous-games');

            // UI EVENTS
            var table = $('#previous-games-tbl');
            $(table).on('hide.bs.collapse', function (e) {
                var placeholderId = '#' + e.target.id;
                var gameName = $(placeholderId).data("name");
                amplify.publish("collapse-previous-game", { gameName: gameName });
            });

            amplify.subscribe("collapse-previous-game", function (p) {
                if (self.data().length > 0) {
                    var entry = ko.utils.arrayFirst(self.data(), function (item) {
                        return p.gameName === item.Name();
                    });
                    if (entry !== null) {
                        entry.DetailData([]);
                    }
                }
            });

            $(table).on('show.bs.collapse', function (e) {
                var placeholderId = '#' + e.target.id;
                var gameName = $(placeholderId).data("name");
                amplify.publish("expand-previous-game", { gameName: gameName });
            });

            amplify.subscribe("expand-previous-game", function (p) {
                if (self.data().length > 0) {
                    var entry = ko.utils.arrayFirst(self.data(), function (item) {
                        return p.gameName === item.Name();
                    });
                    if (entry !== null) {
                        if (entry.Status() === 'Drawed') {

                            $.ajax({
                                url: "/api/game/bets/past/" + encodeURIComponent(p.gameName),
                                type: "GET",
                                contentType: "application/json; charset=utf-8",
                                dataType: "json"
                            }).done(function (data) {

                                common.show(true, 'previous-games-details-' + p.gameName);
                                entry.DetailData([]);
                                entry.DetailData(data);
                                common.show(false, 'previous-games-details-' + p.gameName);
                            });
                        }
                    }
                }
            });
        });
    },
    template: { element: 'previous-games-tmpl' }
});