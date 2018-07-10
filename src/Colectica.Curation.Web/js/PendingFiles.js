function PendingFilesViewModel(filesJson) {
    var self = this;

    var obj = JSON.parse(filesJson);

    self.catalogRecords = ko.observableArray(ko.utils.arrayMap(obj.CatalogRecords, function (x) {
        return ko.mapping.fromJS(x);
    }))

    self.rejectReason = ko.observable();
    self.rejectMessage = ko.observable();

    self.accept = function (catalogRecord) {
        $.ajax({
            type: "POST",
            url: "/File/Accept/",
            data: ko.toJSON({
                model: self
            }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (data) {
                location.reload();
            },
            error: function (data, status, error) {
                location.reload();
            }
        });
    };

    self.reject = function (file) {
        $.ajax({
            type: "POST",
            url: '/File/Reject/',
            data: ko.toJSON({
                model: self
            }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (data) {
                location.reload();
            }
        });
    };

    self.promptToReject = function (file) {
        $("#rejectModal").modal('show');
    };

    self.clickRow = function (file) {
        file.IsChecked(!file.IsChecked());
    };
}