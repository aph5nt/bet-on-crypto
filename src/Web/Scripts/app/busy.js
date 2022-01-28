ko.components.register('busy', {
    viewModel: function (params) {
        var self = this;
        self.show = ko.observable(true);
        self.name = params.name;
       
        if (params.show) {
            self.show = ko.observable(params.show);
        }

        if (params.data) {
            self.name = self.name + '-' + params.data.Name();
        }

        amplify.subscribe("busy", function (p) {
            if (p.name === self.name) {
                self.show(p.show);
            }
        });
        
    },
    template: { element: 'busy-tmpl' }
});


