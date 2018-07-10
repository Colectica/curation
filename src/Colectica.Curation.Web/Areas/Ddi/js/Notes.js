function NotesViewModel(id, url) {
    var self = this;

    self.id = id
    self.url = url;

    self.newNoteText = ko.observable();

    self.newNote = function () {

        // Don't submit empty comments.
        if (!self.newNoteText() || self.newNoteText().length === 0) {
            return;
        }

        // Show a progress bar.
        $("#progress").removeClass('hidden');

        $.post(
            self.url + "/" + self.id,
            { text: self.newNoteText() },
            function (data) {
                // Hide the progress bar.
                $("#progress").addClass("hidden");

                // Clear the text input.
                self.newNoteText("");

                // Refresh the comments view.
                location.reload();
            }
        );
    }
}