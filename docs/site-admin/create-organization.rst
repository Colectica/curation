Create an Organization
========================

The curation tools support hosting multiple organizations on a single
deployment. Each organization's site is accessed from a host name
unique to that organization, but only one deployment of the software
is required.

Prerequisites
--------------------

DNS
~~~

Before configuring a new organization, a DNS hostname should exist, and should point to the IP address of the server on which the curation tools are deployed.

Web Server
~~~~~~~~~~

Within the web server, the curation web application must be configured
to accept connections on the hostname described above.

Curation Tools Configuration
------------------------------

1. Click the :guilabel:`Site Admin` link in the navigation bar.

   .. image:: navigation-admin.png

2. Click the :guilabel:`Organizations` link.

   .. image:: admin-organizations-link.png

3. Click the :guilabel:`Create a New Organization` button.

   .. image:: create-new-organization-button.png

4. Complete the :guilabel:`Create an Organization` form.

   .. image:: create-organization-form.png

   Organization Name
       The name of the organization.
   Host Name
       The DNS host name used to access the organization-specific
       curation site. If multiple organizations exist, this field is
       required. The web server must be configured to listen for
       requests on this host name.
   First Name
       The first name of the organization's administrative user.
   Last Name
       The last name of the organization's administrative user.
   Email Address
       The email address of the organization's administrative user.
   Password
       The password for the organization's administrative user.
   
5. Press the :guilabel:`Create` button.

   .. image:: create-organization-button.png

6. Your organization will be created.
