-----------------------------
Configure the Curation System
-----------------------------

#. Open the Curation Web Application using a web browser.
   Ensure all appropriate system components are working.

   .. image:: curation-web-first-run-components.png

              
#. Click :guilabel:`Initial Configuration` to create an organization and administrative user.

   .. image:: curation-web-initial-configuration.png

   Site Name
       Enter the hostname as the site name
   Organization Name
       Enter the name of the organization
   Agency ID
       Use your domain name if you do not have a DDI agency identifier

       .. seealso::

          To obtain a DDI agency identifier, visit
          http://registry.ddialliance.org/

   Administrator Email Address
       Enter the email address of the main administrator.
       This will also be the username for the administrator
       account.
   Password
       Enter the password for the main administrator account.
   
#. To test that the site was succesfully configured, log in to the
   site using the administrator account.

   .. image:: curation-web-first-login.png

   After logging in you will be taken to the Administration area,
   where you can configure settings and create new users.

   .. image:: curation-web-admin.png


#. Click Site Settings to configure storage locations.

   The :guilabel:`Ingest Directory`, :guilabel:`Processing Directory`,
   and :guilabel:`Archive Directory` are required for proper
   functionality.

   .. image:: curation-web-empty-site-settings.png
   
   .. seealso::

      See the :doc:`/curation/site-admin/index` for more
      information on configuring the curation tools.
