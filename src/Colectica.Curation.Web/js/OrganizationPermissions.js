function OrganizationPermissionsViewModel(
    organizationId,
    organizationName,
    allOrganizationUsersJson,
    usersWithCanAssignRightsJson,
    usersWithCanViewAllCatalogRecordsJson,
    usersWithCanAssignCuratorsJson,
    usersWithCanEditOrganizationJson,
    usersWithCanApproveJson) {

    var self = this;

    self.organizationId = ko.observable(organizationId);
    self.organizationName = ko.observable(organizationName);

    var allUsersList = $.parseJSON(allOrganizationUsersJson);
    self.allUsers = ko.observableArray(allUsersList);

    var assignRightsList = $.parseJSON(usersWithCanAssignRightsJson);
    self.canAssignRightsUsers = ko.observableArray(assignRightsList);

    var viewAllList = $.parseJSON(usersWithCanViewAllCatalogRecordsJson);
    self.canViewAllCatalogRecordsUsers = ko.observableArray(viewAllList);

    var assignCuratorsList = $.parseJSON(usersWithCanAssignCuratorsJson);
    self.canAssignCuratorsUsers = ko.observableArray(assignCuratorsList);

    var canEditOrganizationList = $.parseJSON(usersWithCanEditOrganizationJson);
    self.canEditOrganizationUsers = ko.observableArray(canEditOrganizationList);

    var canApproveList = $.parseJSON(usersWithCanApproveJson);
    self.canApproveUsers = ko.observableArray(canApproveList);

    ko.bindingHandlers.sortable.options = {
        start: function (event, ui) {
            ui.item.addClass('tilt');
        },

        stop: function (event, ui) {
            ui.item.removeClass('tilt');
        }
    };

    self.addToCanAssignRights = function (args, event, ui) {
        var selfArgs = args;
        var exists = _.any(self.canAssignRightsUsers(), function (x) {
            return x.UserName == selfArgs.item.UserName;
        });

        if (exists)
        {
            args.cancelDrop = true;
            return;
        }

        $.post(
            "/Organization/AddCanAssignRights",
            {
                OrganizationId: self.organizationId,
                UserName: args.item.UserName
            });
    };

    self.addToCanViewAllCatalogRecords = function (args, event, ui) {
        var selfArgs = args;
        var exists = _.any(self.canViewAllCatalogRecordsUsers(), function (x) {
            return x.UserName == selfArgs.item.UserName;
        });

        if (exists) {
            args.cancelDrop = true;
            return;
        }

        $.post(
            "/Organization/AddCanViewAllCatalogRecords",
            {
                OrganizationId: self.organizationId,
                UserName: args.item.UserName
            });
    };

    self.addToCanAssignCurators = function (args, event, ui) {
        var selfArgs = args;
        var exists = _.any(self.canAssignCuratorsUsers(), function (x) {
            return x.UserName == selfArgs.item.UserName;
        });

        if (exists) {
            args.cancelDrop = true;
            return;
        }

        $.post(
            "/Organization/AddCanAssignCurators",
            {
                OrganizationId: self.organizationId,
                UserName: args.item.UserName
            });
    };

    self.addToCanEditOrganization = function (args, event, ui) {
        var selfArgs = args;
        var exists = _.any(self.canEditOrganizationUsers(), function (x) {
            return x.UserName == selfArgs.item.UserName;
        });

        if (exists) {
            args.cancelDrop = true;
            return;
        }

        $.post(
            "/Organization/AddCanEditOrganization",
            {
                OrganizationId: self.organizationId,
                UserName: args.item.UserName
            });
    };

    self.addToCanApprove = function (args, event, ui) {
        var selfArgs = args;
        var exists = _.any(self.canApproveUsers(), function (x) {
            return x.UserName == selfArgs.item.UserName;
        });

        if (exists) {
            args.cancelDrop = true;
            return;
        }

        $.post(
            "/Organization/AddCanApprove",
            {
                OrganizationId: self.organizationId,
                UserName: args.item.UserName
            });
    };



    
}