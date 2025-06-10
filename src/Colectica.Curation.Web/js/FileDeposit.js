function FileViewModel(date, name, size, type, title, source, sourceInformation, rights, publicAccess, allExistingFileNames) {
    var self = this;

    self.date = ko.observable(date);
    self.name = ko.observable(name);
    self.size = ko.observable(size);
    self.type = ko.observable(type);
    self.title = ko.observable(title);
    self.source = ko.observable(source);
    self.sourceInformation = ko.observable(sourceInformation);
    self.rights = ko.observable(rights);
    self.publicAccess = ko.observable(publicAccess);

    self.isTypeAutoDetected = ko.observable();

    self.extension = getFileExtension(self.name());

    self.isNameAlreadyPresent = ko.observable( ($.inArray(self.name(), allExistingFileNames()) === 0 ));

    self.selectedExistingFileName = ko.observable(name);

    self.shouldShowLargeDataFileWarning = ko.computed(function () {
        return self.size() > (20 * 1024 * 1024) && self.type() == "Data";
    });

    // Add any existing file names, if the extension matches.
    self.existingFileNames = ko.observableArray(
        _.filter(allExistingFileNames(), function (item) {
            return getFileExtension(item) === self.extension;
        })
    );

    self.friendlySize = bytesToSize(size);

}

function FileDepositViewModel(existingFileNamesJson, hasAgreement) {
    var self = this;

    this.files = ko.observableArray([]);
    self.hasAgreement = ko.observable(hasAgreement);
    self.isAgreementChecked = ko.observable();

    self.isUploading = ko.observable();
    self.errorMessage = ko.observable();
    self.notes = ko.observable();

    var existingFileNamesArray = JSON.parse(existingFileNamesJson);
    self.existingFileNames = ko.observableArray(existingFileNamesArray);


    self.canUpload = ko.computed(function () {
        if (self.isUploading()) {
            return false;
        }

        return true;

        //if (!self.hasAgreement()) {
        //    return true;
        //}

        //if (self.isAgreementChecked()) {
        //    return true;
        //}

        //return false;
    });



    // Get the template HTML for dropzone and remove it from the doument
    var previewNode = document.querySelector("#template");
    previewNode.id = "";
    var previewTemplate = previewNode.parentNode.innerHTML;
    previewNode.parentNode.removeChild(previewNode);

    Dropzone.autoDiscover = false;

    var myDZ = $("#myDropzone").dropzone({
        url: "/CatalogRecord/Deposit",
        previewTemplate: previewTemplate,
        autoProcessQueue: false,
        uploadMultiple: true,
        parallelUploads: 100,
        maxFilesize: 1000,
        maxFiles: 100,
        previewsContainer: "#previews",
        clickable: ".fileinput-button",

        init: function () {
            var dz = this;

            this.on("addedfile", function (file) {

                // If any existing files of the same type have 
                // data type, data source, or data source information,
                // set these here for the new file.
                var fileType = getFileType(file.name);

                var source = self.getFirstSourceForType(fileType);
                var sourceInformation = "";
                var rights = "";
                var publicAccess = "Yes";

                var hasExistingMatch = _.any(self.existingFileNames(), function (x) {
                    return x === file.name;
                });
                if (hasExistingMatch) {
                    $("#ActionType").val("New Version");
                    $("#ActionType").attr('disabled', 'disable');


                }

                var newFileVM = new FileViewModel(
                    file.lastModifiedDate,
                    file.name,
                    file.size,
                    fileType,
                    file.name,
                    source,
                    sourceInformation,
                    rights,
                    publicAccess,
                    self.existingFileNames);
                if (fileType !== "Supplementary Materials") {
                    newFileVM.isTypeAutoDetected(true);
                }
                self.files.push(newFileVM);

                // Watch for changes to the source property.
                newFileVM.source.subscribe(function (newSource) {
                    // If any files of the same type do not have a source,
                    // automatically apply this source to those files.
                    var filesWithoutSource = _.filter(self.files(), function (file) {
                        return file.type() === newFileVM.type() && file.source() == "";
                    });

                    filesWithoutSource.forEach(function (file) {
                        file.source(newSource);
                    });

                }, newFileVM);
            });

            this.on("complete", function (file) {
                if (file.status != "error" &&
                    this.getUploadingFiles().length === 0 && this.getQueuedFiles().length === 0) {
                    // When done uploading, redirect to the file list.
                    var id = document.location.href.substr(document.location.href.lastIndexOf('/') + 1);
                    document.location.href = "/CatalogRecord/Files/" + id;
                }
            });

            // Update the total progress bar
            this.on("totaluploadprogress", function (progress) {
                document.querySelector("#total-progress .progress-bar").style.width = progress + "%";
            });

            this.on("sending", function (file) {
                console.log(file);
            });

            this.on("error", function (file, message) {
                self.errorMessage(message);
            });

            // Hide the total progress bar when nothing's uploading anymore
            this.on("queuecomplete", function (progress) {
                self.isUploading(false);
            });

            var submitButton = document.getElementById("submitButton");
            submitButton.addEventListener("click", function (e) {

                e.preventDefault();
                e.stopPropagation();

                var hasExistingMatch = _.any(self.files(), function (x) {
                    return x.selectedExistingFileName() !== "";
                });

                if (hasExistingMatch && !self.notes()) {
                    self.errorMessage("Please enter some notes about the files.");
                    return;
                }


                self.isUploading(true);

                //tell Dropzone to process all queued files
                dz.processQueue();
            });

            self.getFirstSourceForType = function (fileType) {
                var file = _.find(self.files(), function (file) {
                    return file.type() === fileType && file.source() !== "";
                });

                if (file) {
                    return file.source();
                }

                return "";
            };
        }
    });


}
