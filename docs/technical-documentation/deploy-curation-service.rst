------------------------------------------
Deploy the Curation Service
------------------------------------------

Step 1. Extract the Curation Service Files
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

#. Extract the curation service to a directory on the processing
   machine. For example, :file:`C:\\ColecticaCurationService\\`.

   .. note::

    Before extracting the ZIP file, make sure Windows does not have
    the file blocked. This may be the case if you downloaded the ZIP
    file over the Internet. To unblock the file, right click the file,
    choose :guilabel:`Properties`, and click the :guilabel:`Unblock`
    button if one exists. If there is no :guilabel:`Unblock` button,
    the file is not blocked.

Step 2. Update Curation Service Data Connection Strings
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

#. If there is not a file named :file:`ConnectionStrings.config`,
   rename :file:`ConnectionStrings.config.dist` to
   :file:`ConnectionStrings.config`
#. Update the connection string named ``ColecticaRepository`` for the
   Colectica Repository database, which should be accessible to the
   service.
#. Update the connection string named ``DefaultConnection`` for the
   Curator database, which must be accessible to the service.

Step 3. Install the Curation Service
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

#. Install the curation service using the install.bat file provided
   with the installation. As an Administrator, run::

     install.bat
    
#. Open the :guilabel:`Services` manager from the
   :guilabel:`Administrative Tools`.

   .. image:: services-colectica-curation-service.png
              
   a. Set the appropriate service account to run the service.
   b. Ensure the service account has the correct disk and database
      permissions.
   c. Ensure the service is set to start automatically.
   d. To start the Curation Service, right click the service and
      choose :menuselection:`Start`.

Step 4. Test
^^^^^^^^^^^^

#. Make sure the Curation Service started in the previous step.
#. Be sure there are no reported errors once you start the service.

    a. If run under the ``Network Service`` account, the service log
       will appear in
       :file:`C:\\Windows\\ServiceProfiles\\NetworkService\\AppData\\Roaming\\Colectica\\Logs`
    b. If run under another account, the service's log will be in
       :file:`%appdata%\\Roaming\\Colectica\\Logs`
