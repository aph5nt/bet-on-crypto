/// <reference path="../knockout-3.3.0.js" />
/// <reference path="common.js" />

ko.components.register('prize', {
    viewModel: function () {

        var self = this;
        self.prize = ko.observable(0);
        self.fiatValue = ko.observable(0);

        self.prizeFmt = ko.computed(function () {
            return self.prize().toString();
        });

        //var convert = function() {
        //    $.ajax({
        //        url: "https://www.bitstamp.net/api/ticker/",
        //        type: "GET",
        //        contentType: "application/json; charset=utf-8",
        //        crossDomain: true,
        //        dataType: 'json'
        //    }).done(function (data) {
        //        var result = self.prize() * parseFloat(data.last);
        //        self.fiatValue(result);
        //    }).fail(function(err) {
        //        console.log(err);
        //    });
        //};

        // SignalR
        var gameHub = $.connection.GameHub;
        gameHub.client.onAccumulationUpdate = function (result) {
            self.prize(result);
           // convert();
        };

        // Initial Load
        $.ajax({
            url: "/api/game/accumulation",
            type: "GET",
            contentType: "application/json; charset=utf-8",
            dataType: "json"
        }).done(function (data) {
            self.prize(data);
            //convert();
        });

    },
    template:  { element: 'prize-tmpl' } 
});

//(<span data-bind="text: fiatValue"/>$)