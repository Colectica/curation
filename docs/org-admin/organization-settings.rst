Organization Settings
=========================

#. To edit an organization's settings, click :menuselection:`Admin -->
   Organizations`, and then click the edit icon for the organization you
   wish to edit.

   .. image:: edit-organization-link.png

#. On the settings page, fill in the appropriate settings and click
   the :guilabel:`Save` button.

General
-------------------------

Name
    The name of the archive. This name will appear on the login
    screen, as the page title, and in other areas of the user
    interface.

Image URL
    The absolute URL to an image of a logo to be displayed on the
    login screen.

Hostname
    The hostname used to access this organization. When accessing
    the curation application via an organization-specific hostname,
    only information related to the corresponding organization will
    be available.

Agency ID
    The DDI agency identifier of the organization.

    .. seealso::

       See http://registry.ddialliance.org/ for more information about
       DDI agency identifiers.

Contact Information
    Contact information for the organization. This information will be
    copied into each catalog record as the :dfn:`owner contact` field.


Storage Locations
-------------------------

Specifying storage locations is optional at the organization level. If
storage locations are not specified here, the site-wide storage
locations are used.

.. seealso::

   See :doc:`/technical-documentation/create-storage-areas`
   for information about provisioning and configuring storage areas.

Ingest Directory
    The disk location where newly ingested files will be stored.

Processing Directory
    The disk location where files will be stored while being curated.

Archive Directory
    The disk location where archive packages containing original files
    and published files will be stored.

Policies
-------------------------

Deposit Agreement
    The text to which depositors must agree before submitting a
    catalog for curation.

    This field supports `Markdown
    <https://daringfireball.net/projects/markdown/syntax>`_
    formatting. Additionally, the following tokens are replaced
    with the appropriate text.

    ====================  ================================================
    Token                 Text
    ====================  ================================================
    @UserName             The email address of the current user
    @TermsOfServiceLink   A link to the terms of service
    @PolicyLink           A link to the citation policy
    @FullName             The name of the current user
    @Title                The title of the catalog record being submitted
    @Date                 The current date
    @check                A checkbox. All checkboxes must be checked in
                          order for the user to submit a catalog record
                          for curation.
    ====================  ================================================

Terms of Service
    The terms of service which must be agreed to when creating a
    new account and when depositing a new catalog record.

Organization Citation Policy
    Specifies how a catalog record should be cited. This information
    will be copied into each catalog record as the :dfn:`citation
    policy` field.

Email
-------------------------

Reply-to Address
    The email address to set in the as the reply-to when sending
    automated notification emails.

Notification Email Closing
    Text to include at the end of all notification emails.


Handle Service
-------------------------

A Handle is a unique and persistent identifier for a digital object.

Configuring a Handle service is optional. If one is configured, the
curation system will automatically request Handle identifiers for
catalog records and all files within a record when the catalog record
is published.

.. seealso::

   See http://www.handle.net/ for more information about using Handles
   as unique and persistent identifiers for digital objects.

Handle Server Address
    The hostname of the Handle server.

Handle Group Name
    The group name to use on the Handle server.

Handle Username
    The username for the Handle service.

Handle Password
    The password for the Handle service.


Account Creation
-------------------------

Allow Anonymous Registration
    If checked, users will be able to register for new accounts from
    the home page. If not checked, new users must be created manually
    by administrators.
