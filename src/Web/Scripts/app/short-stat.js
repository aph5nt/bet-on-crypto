/// <reference path="../knockout-3.3.0.js" />
/// <reference path="common.js" />

ko.components.register('short-stat', {
    viewModel: function () {

        var self = this;
        self.TotalWon = ko.observable(0);
        self.HighestWin = ko.observable(0);
        self.TotalWins = ko.observable(0);


        self.TotalWonFmt = ko.computed(function () {
            return self.TotalWon().toString();
        });

        self.HighestWinFmt = ko.computed(function () {
            return self.HighestWin().toString();
        });

        // SignalR
        var gameHub = $.connection.GameHub;
        gameHub.client.onShortStatUpdate = function (result) {
            self.TotalWon(result.TotalWon);
            self.HighestWin(result.HighestWin);
            self.TotalWins(result.TotalWins);
        };

        // Initial Load
        $.ajax({
            url: "/api/game/shortstats",
            type: "GET",
            contentType: "application/json; charset=utf-8",
            dataType: "json"
        }).done(function (data) {
            self.TotalWon(data.TotalWon);
            self.HighestWin(data.HighestWin);
            self.TotalWins(data.TotalWins);
        });
    },
    template: { element: 'short-stat-tmpl' }
});
