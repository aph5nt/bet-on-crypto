/// <reference path="../knockout-3.3.0.js" />
/// <reference path="../jquery-2.1.4.min.js" />
/// <reference path="../plugins/toastr/toastr.min.js" />
/// <reference path="../amplify.min.js" />

ko.components.register('balance', {
    viewModel: function (params) {
        var self = this;
        self.Loading = ko.observable(true);
        self.Balance = ko.observable(0);
        self.Bets = ko.observable(0);
        self.IsBetPlaced = ko.observable(false);

        window.common.address = params.address;

        var showLoadingIndicator = function(show) {
            self.Loading(show);
            amplify.publish("busy", { name: 'balance', show: show });
        }

        $.connection.hub.logging = true;
        $.connection.hub.qs = "address=" + params.address;
        var balanceHub = $.connection.BalanceHub;

        balanceHub.client.onBalanceUpdated = function (actualBalance) {
            self.Balance(actualBalance);
            self.Bets(Math.round(actualBalance / params.bet));
        }

        $.connection.hub.start().done(function() {
            console.log('Now connected, connection ID=' + $.connection.hub.id);
        });

        var queryCommandEnabled = function (address) {
            showLoadingIndicator(true);
            $.ajax({
                url: "/api/wallet/balance/" + address,
                type: "GET",
                beforeSend: function (xhr) {
                    var token = $('input[name="__RequestVerificationToken"]').attr('value');
                    xhr.setRequestHeader('X-XSRF-Token', token);
                },
                contentType: "application/json; charset=utf-8",
                dataType: "json"
            }).done(function (data) {
                
                // after bet placed and changed balance
                if (self.IsBetPlaced() && data.Balance < self.Balance()) {
                    self.Balance(data.Balance);
                    self.Bets(Math.round(data.Bets));
                    self.IsBetPlaced(false);
                    showLoadingIndicator(false);
                }

               // after bet placed but when the balance remains the same
                else if (self.IsBetPlaced()) {
                    // do nothing
                // no bet placed just update the data
                } else {
                    self.Balance(data.Balance);
                    self.Bets(Math.round(data.Bets));
                    showLoadingIndicator(false);
                }
            }).fail(function(err) {
                showLoadingIndicator(false);
            });
        }

        queryCommandEnabled(params.address, false);
    },
    template: { element: 'balance-tmpl' }
});
