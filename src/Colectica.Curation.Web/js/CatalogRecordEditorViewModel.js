function CatalogRecordEditorViewModel(catalogRecordId, organizationName, authorsJson, availableOrganizationNames, hasAgreement) {
    var self = this;

    self.organizationName = ko.observable(organizationName);

    self.hasAgreement = ko.observable(hasAgreement);
    self.selectedOrganizationName = ko.observable(organizationName);

    self.isAgreementChecked = function () {
        return $('.agreement-checkbox:checked').length == $('.agreement-checkbox').length;
    };

    self.peopleInput = ko.observable(new PeopleInputViewModel(self.selectedOrganizationName, authorsJson));

    self.submitAuthors = function () {
        $.post("/CatalogRecord/General",
        {
            pk: catalogRecordId,
            name: "Authors",
            value: ko.toJSON(self.peopleInput().people())
        },
        function (data) {
            self.hideAuthorField();
        });
    };

    self.canCreate = ko.computed(function () {
        if (!self.hasAgreement()) {
            return true;
        }

        if (self.isAgreementChecked()) {
            return true;
        }

        return false;
    });
}