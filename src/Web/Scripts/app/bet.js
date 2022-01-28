/// <reference path="../knockout-3.3.0.js" />
/// <reference path="../jquery-2.1.4.min.js" />
/// <reference path="../amplify.min.js" />
/// <reference path="../plugins/toastr/toastr.min.js" />
 
ko.components.register('bet', {
    viewModel: function () {

        var self = this;
       
        self.Vote = ko.observable(0.00);
        self.InProgress = ko.observable(false);
        self.IsNotNumeric = ko.computed(function () {
            var result = !$.isNumeric(self.Vote());
            return result;
        });

        self.IsPlaceBetEnabled = ko.computed(function() {
            return !self.InProgress() && !self.IsNotNumeric();
        });

        self.PlaceBet = function () {
 
            var vote = self.Vote();
            self.InProgress(true);
            $.ajax({
                url: "/api/wallet/bet",
                beforeSend: function (xhr) {
                    var token = $('input[name="__RequestVerificationToken"]').attr('value');
                    xhr.setRequestHeader('X-XSRF-Token', token);
                },
                type: "POST",
                contentType: "application/json; charset=utf-8",
                data: ko.toJSON({ Vote: vote }),
                dataType: "json"
            }).done(function (data) {
                toastr["success"](data.responseText);
            }).fail(function (err) {
                toastr["error"](err.responseText);
            });
          
            self.InProgress(false);
        };
    },
    template: { element: 'bet-tmpl' }
});
