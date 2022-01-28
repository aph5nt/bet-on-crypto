/// <reference path="../knockout-3.4.0.js" />
/// <reference path="../jquery-2.2.1.js" />
/// <reference path="../amplify.min.js" />
/// <reference path="common.js" />
/// <reference path="../plugins/toastr/toastr.min.js" />

ko.components.register('withdraw', {
    viewModel: function () {

        var self = this;
        self.DestinationAddress = ko.observable('');
        self.Password = ko.observable('');
        self.Amount = ko.observable(0.0);
        self.ErrorMessages = ko.observableArray([]);
        var hide = function () {
            $('#withdrawModal').modal('hide');
        };

        self.Cancel = function () {
            self.DestinationAddress('');
            self.Password('');
            self.Amount(0);
            self.ErrorMessages([]);
        };

        self.Withdraw = function () {

            var destinationAddress = self.DestinationAddress();
            var password = self.Password();
            var amount = self.Amount();

            self.ErrorMessages([]);

            var isvalid = $('#withdrawForm')[0].checkValidity();
            if (!isvalid) {
                if (destinationAddress.length === 0) {
                    self.ErrorMessages.push('Destination address is required.');
                }
                if (password.length === 0) {
                    self.ErrorMessages.push('Password is required.');
                }
                if (amount.length === 0) {
                    self.ErrorMessages.push('Amount is required.');
                }

                return false;
            }

            $.ajax({
                url: "/api/wallet/withdraw",
                type: "POST",
                beforeSend: function (xhr) {
                    var token = $('input[name="__RequestVerificationToken"]').attr('value');
                    xhr.setRequestHeader('X-XSRF-Token', token);
                },
                contentType: "application/json; charset=utf-8",
                data: ko.toJSON({ Amount: amount, DestinationAddress: destinationAddress, Password: password }),
                dataType: "json"
            }).done(function (data) {
                self.Password("");
                toastr["success"](data.responseText);
            }).fail(function (err) {
                self.Password("");
                toastr["error"](err.responseText);
            });

            hide();

            return false;
        };
    },
    template: { element: 'withdraw-tmpl' }
});
