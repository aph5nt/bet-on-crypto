/// <reference path="../knockout-3.4.0.js" />
/// <reference path="../jquery-2.2.1.js" />
/// <reference path="../amplify.min.js" />
/// <reference path="common.js" />
/// <reference path="../plugins/toastr/toastr.min.js" />

ko.components.register('deposit', {
    viewModel: function (params) {
        var self = this;
        self.Address = ko.observable(params.address);
        self.Chart = "https://chart.googleapis.com/chart?chs=400x400&cht=qr&chl=" + params.address;
    },
    template: { element: 'deposit-tmpl' }
});
