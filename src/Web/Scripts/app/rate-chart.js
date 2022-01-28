/// <reference path="../moment.js" />

ko.components.register('rate-chart', {
    viewModel: function () {

        // Dates
        var dateFormat = 'HH:mm';
        var formatDate = function (date) {
            var localTime = moment.utc(date).toDate();
            return moment(localTime).format(dateFormat);
        };
        var processRates = function (rates) {
            for (var i = 0; i < rates.labels.length; i++) {
                rates.labels[i] = formatDate(rates.labels[i]);
            }
            return rates;
        };

        // Chart
        var canvas = document.getElementById("rateChart");
        var ctx = canvas.getContext("2d");

        var chart = new Chart(ctx);
        var lineChart = null;

        // SignalR
        var rateChartsHub = $.connection.RateChartHub;
        rateChartsHub.client.broadcastData = function (rate) {
            if (lineChart === null || lineChart.removeData === null)
                return;

            lineChart.stop();
            lineChart.addData([rate.Data[0]], formatDate(rate.Label));
            lineChart.removeData();
            lineChart.render();
        };

        // Initial Load
        $.ajax({
            url: "/api/game/rates",
            type: "GET",
            contentType: "application/json; charset=utf-8",
            dataType: "json"
        }).done(function (rates) {

            var options = {
                maintainAspectRatio: true,
                responsive: false,
                multiTooltipTemplate: "<%= datasetLabel %>  <%= value %>%"
            };
            var data = {
                labels: rates.Labels,
                datasets: [
                    {
                        label: "7d",
                        data: rates.DataSets[0].Data,
                        fillColor: "rgba(100,187,205,0.2)",
                        strokeColor: "rgba(100,187,205,1)",
                        pointColor: "rgba(100,187,205,1)",
                        pointStrokeColor: "#fff",
                        pointHighlightFill: "#fff",
                        pointHighlightStroke: "rgba(151,187,205,1)"
                    }
                ]
            };
            lineChart = chart.Line(processRates(data), options);
        });
    },
    template: '<canvas id="rateChart" width="1080px"  height="240px"></canvas>'
});