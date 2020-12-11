

function VariableEditorViewModel(fileId, hasBottomDrawer, variablesJson) {
    var self = this;

    self.fileId = fileId;

    var variablesList = $.parseJSON(variablesJson);

    self.variables = ko.observableArray(ko.utils.arrayMap(variablesList, function (v) {
        //return new VariableViewModel(v.Id, v.Name, v.Label);
        return ko.mapping.fromJS(v);
    }))

    self.selectedVariable = ko.observable();
    self.selectedVariableId = ko.observable();
    self.selectedVariableName = ko.observable();
    self.selectedListItem = null;

    self.newNoteText = ko.observable();

    // HACK
    var subtract = 250;

    $(".missingLabelsVariableEditorWrapper").height(window.innerHeight - subtract);
    $(".variableEditorWrapper").height(window.innerHeight - subtract);
    window.onresize = function(event) {
        $(".missingLabelsVvariableListWrapper").height(window.innerHeight - subtract);
        $(".variableEditorWrapper").height(window.innerHeight - subtract);
    };

    $.fn.editable.defaults.success = function (response, newValue) {
        self.selectNode(self.selectedListItem);
    };

    self.selectNode = function (node) {
        self.selectedListItem = node;

        // Immediately show the name and progress bar.
        // Hide any variable details that were previously shown.
        self.selectedVariableId(node.Id());
        self.selectedVariableName(node.Name());

        self.isLoading = true;
        setTimeout(function () {
            if (self.isLoading) {
                $("#variable-progress").removeClass("hidden");
            }
        }, 400);

        self.selectedVariable(null);
        
        // Get full variable details from the server.
        $.get("/Variables/Details/",
            {
                id: node.Id,
                agency: node.Agency,
                fileId: self.fileId
            },
            function gotVariableDetails(data) {
                self.isLoading = false;
                $("#variable-progress").addClass("hidden");

                // Got the details, set the selected variable.
                var variableVM = ko.mapping.fromJSON(data);
                
                self.selectedVariable(variableVM);
                self.newNoteText("");

                $(".info-tip").tooltip({
                    'placement': 'top',
                    'container': 'body'
                });
            });
    }

    self.newNote = function () {
        // Don't submit empty comments.
        if (!self.newNoteText() || self.newNoteText().length === 0) {
            return;
        }

        // Show a progress bar.
        $("#comment-progress").removeClass('hidden');

        $.post(
            "/Variables/Editor/AddNote/" + self.selectedVariable().Id(),
            {
                note: self.newNoteText(),
                fileId: self.fileId
            },
            function (data) {
                // Hide the progress bar.
                $("#comment-progress").addClass("hidden");


                // Refresh the comments view.
                self.selectedVariable().Comments.push(data);

                // Clear the text input.
                self.newNoteText("");
            }
        );
    };

    self.dataTypes = ko.observableArray([
            { value: "Text", text: "Text" },
            { value: "Numeric", text: "Numeric" },
            { value: "Code", text: "Code" }
    ]);

    self.onUpdateLabelSuccess = function (response, newValue) {
        self.selectedListItem.Label(newValue);
        self.selectedListItem.IsLabelOk('true');
        self.selectNode(self.selectedListItem);
    };

    self.onUpdateRepresentationTypeSuccess = function(response, newValue) {
        self.selectNode(self.selectedListItem);
        
    };

    self.onUpdateCategoryLabelSuccess = function (response, newValue) {
        console.log(response);
        console.log(newValue);

    };


    self.responseUnits = ko.observableArray([
                                { value: 'Self', text: 'Self (unit of response is the same as unit of observation/analysis)' },
                                { value: 'Informant', text: 'Informant (e.g., physician, different organizational department)' },
                                { value: 'Proxy', text: 'Proxy (e.g., different member of household)' },
                                { value: 'Researcher', text: 'Researcher (i.e., as result of personal)' },
                                { value: 'Other', text: 'Other' }, { value: 'Housing Unit', text: 'Housing Unit' }
    ]);

    self.analysisUnits = ko.observableArray([
                                { value: 'Individual', text: 'Individual' },
                                { value: 'Organization', text: 'Organization' },
                                { value: 'Household', text: 'Household' },
                                { value: 'Event', text: 'Event' },
                                { value: 'Geographic unit', text: 'Geographic unit' },
                                { value: 'Text unit', text: 'Text unit' },
                                { value: 'Time unit', text: 'Time unit' },
                                { value: 'Group', text: 'Group' },
                                { value: 'Object', text: 'Object' },
                                { value: 'Other', text: 'Other' },
                                { value: 'Housing Unit', text: 'Housing Unit' }
    ]);

}
