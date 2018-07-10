function CatalogRecordPermissionsViewModel(
    catalogRecordId,
    allOrganizationUsersJson,
    curatorsJson,
    approversJson) {

    var self = this;

    self.catalogRecordId = ko.observable(catalogRecordId);

    var allUsersList = $.parseJSON(allOrganizationUsersJson);
    self.allUsers = ko.observableArray(allUsersList);

    var curatorsList = $.parseJSON(curatorsJson);
    self.curators = ko.observableArray(curatorsList);

    var approversList = $.parseJSON(approversJson);
    self.approvers = ko.observableArray(approversList);


    ko.bindingHandlers.sortable.options = {
        start: function (event, ui) {
            ui.item.addClass('tilt');
        },

        stop: function (event, ui) {
            ui.item.removeClass('tilt');
        }
    };

    self.addToCurators = function (args, event, ui) {
        var selfArgs = args;
        var exists = _.any(self.curators(), function (x) {
            return x.UserName == selfArgs.item.UserName;
        });

        if (exists) {
            args.cancelDrop = true;
            return;
        }

        $.post(
            "/CatalogRecord/AddCurator",
            {
                CatalogRecordId: self.catalogRecordId,
                UserName: args.item.UserName
            });
    };

    self.addToApprovers = function (args, event, ui) {
        var selfArgs = args;
        var exists = _.any(self.approvers(), function (x) {
            return x.UserName == selfArgs.item.UserName;
        });

        if (exists) {
            args.cancelDrop = true;
            return;
        }

        $.post(
            "/CatalogRecord/AddApprover",
            {
                CatalogRecordId: self.catalogRecordId,
                UserName: args.item.UserName
            });
    };


}