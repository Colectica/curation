function PersonViewModel() {
    var self = this;

    self.id = ko.observable();
    self.firstName = ko.observable();
    self.lastName = ko.observable();
    self.affiliation = ko.observable();
    self.email = ko.observable();
    self.contactInformation = ko.observable();
    self.orcid = ko.observable();

    self.errorMessage = ko.observable();

    this.fullName = ko.computed(function () {
        if (self.firstName() == undefined && self.lastName() == undefined) {
            return "";
        }
        else if (self.firstName() == undefined)
        {
            return self.lastName();
        }
        else if (self.lastName() == undefined) {
            return self.firstName();
        }

        return self.firstName() + " " + self.lastName();
    });

    self.initialize = function () {
        
        var suggestionTemplate = function (context) {
            var str = "";

            str += "<div class='author-suggestion'><div class='row'><div class='col-sm-1'>";

            if (context.IsVerified) {
                str += "<span class='glyphicon glyphicon-user'></span>"
            }

            str += "</div>";

            str += "<div class='col-sm-11' style='overflow-x: hidden;'>";

            if (context.FullName &&
                context.FullName !== "") {
                str += "<strong>" + context.FullName + "</strong>";
            }

            if (context.Email &&
                context.Email !== "") {
                str += "<br/><small>" + context.Email + "</small>";
            }

            if (context.Orcid &&
                context.Orcid !== "") {
                str += "<br/><small>" + context.Orcid + "</small>";
            }

            str += "</div></div></div>";

            return str;
        };

        var emptyTemplate = function (context) {
            str = "<div class='author-suggestion'>";
            str += "<p>No results found for " + context.query;
            str += "<p><a href='#createAuthorModal' data-toggle='modal' class='btn btn-primary'>Create " + context.query + "</a>";
            str += "</div>";

            return str;
        };

        var footerTemplate = function (context) {

            if (context.isEmpty) {
                return "";
            }

            str = "<div class='author-suggestion'>";
            str += "<hr />"
            str += "<p>Couldn't find who you're looking for?";
            str += "<p><a href='#createAuthorModal' data-toggle='modal' class='btn btn-primary'>Create a new user</a>";
            str += "</div>";

            return str;
        };

        var usersEngine = new Bloodhound({
            datumTokenizer: Bloodhound.tokenizers.obj.whitespace('value'),
            queryTokenizer: Bloodhound.tokenizers.whitespace,
            remote: '/User/Search/%QUERY'
        });

        usersEngine.initialize();

        $('.author-input').typeahead({
            hint: false,
            highlight: true,
            minLength: 1,
            remote: "/User/Search/%QUERY",
            displayKey: "FullName",
            remote: 'users'
        },
        {
            source: usersEngine.ttAdapter(),
            name: 'users',
            displayKey: 'FullName',
            templates: {
                empty: emptyTemplate,
                suggestion: suggestionTemplate,
                footer: footerTemplate
            }
        });

        $('.author-input').bind('typeahead:selected', function (obj, person, name) {
            self.id(person.Id);
            self.firstName(person.FirstName);
            self.lastName(person.LastName);
            self.affiliation(person.Affiliation);
            self.email(person.Email);
            self.orcid(person.Orcid);
        });
    }


}

function PeopleInputViewModel(organizationName, peopleJson) {
    var self = this;

    self.organizationName = organizationName;
    
    // Load existing people from actual data
    self.people = ko.observableArray([]);


    if (peopleJson) {
        var peopleArr = JSON.parse(peopleJson);
        _.each(peopleArr, function (x) {
            var person = new PersonViewModel();
            person.id(x.Id);
            person.firstName(x.FirstName);
            person.lastName(x.LastName);
            person.affiliation(x.Affiliation);
            person.email(x.Email);
            person.contactInformation(x.ContactInformation);
            person.orcid(x.Orcid);
            self.people.push(person);
        });
    }    

    self.newPerson = ko.observable(new PersonViewModel());

    self.namesList = ko.computed(function () {
        var names = _.map(self.people(), function (x) { return x.fullName(); });
        return names.join(", ");
    });

    self.isEmpty = ko.computed(function () {
        if (self.namesList().length == 0) {
            return true;
        }

        return false;
    });

    self.createNewPlaceholderAccount = function () {

        $.post(
            "/User/CreatePlaceholder",
            {
                FirstName: self.newPerson().firstName(),
                LastName: self.newPerson().lastName(),
                Affiliation: self.newPerson().affiliation(),
                Email: self.newPerson().email(),
                Orcid: self.newPerson().orcid(),
                OrganizationName: self.organizationName()
            },
            function newUserCreated(data) {
                self.newPerson().errorMessage("");

                // Remove any lines that don't have an ID.
                var empties = _.filter(self.people(), function (x) {
                        return x.id() == undefined
                    });
                var empty = _.each(empties,
                    function (empty) {
                        self.people.remove(empty);
                    });
                                
                var createdPerson = new PersonViewModel();
                createdPerson.id(data.Id);
                createdPerson.firstName(data.FirstName);
                createdPerson.lastName(data.LastName);
                createdPerson.affiliation(data.Affiliation);
                createdPerson.email(data.Email);
                createdPerson.orcid(data.Orcid);

                self.people.push(createdPerson);

                self.newPerson().firstName("");
                self.newPerson().lastName("");
                self.newPerson().affiliation("");
                self.newPerson().email("");
                self.newPerson().orcid("");

                $("#createAuthorModal").modal("hide");
            })
        .fail(function () {
            self.newPerson().errorMessage("The user could not be created. A user with this email may already exist.");
        });
    };

    self.addBlankPersonLine = function () {
        self.people.push(new PersonViewModel());
    };

    self.removePerson = function (person) {
        self.people.remove(person);
    };

    self.initialize = function () {

    };
}

