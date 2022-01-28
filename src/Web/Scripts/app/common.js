
window.common = (function () {
    var common = {};
    common.dateFormat = 'YYYY-MM-DD HH:mm';
    common.dateFormatss = 'YYYY-MM-DD HH:mm:ss';
    common.formatDate = function (date) {
        var localTime = moment.utc(date).toDate();
        return moment(localTime).format(common.dateFormat);
    };
    common.formatDatess = function (date) {
        var localTime = moment.utc(date).toDate();
        return moment(localTime).format(common.dateFormatss);
    };
 
    common.show = function (show, name) {
        amplify.publish("busy", { show: show, name: name });
    };
 
    return common;
})(); 