-----------------------
Provision Storage Areas
-----------------------

Disk storage spaces are used for managing and archiving the curated
content. There are several storage spaces that will be accessed by the
web application, curation service, and third party applications.

A storage space must be accessible by a fully qualified path that is
the same for the web application, curation service, and third party
applications.

If all storage areas are provisioned on the same location, they can be
differentiated using folders instead of mapping drives. For example,
:file:`z:\ingest`, :file:`z:\processing`, :file:`z:\archive`, instead
of :file:`i:\\`, :file:`p:\\`, and :file:`r:\\`.

.. note::

   UNC paths may be acceptable. This needs to be tested to ensure it
   works with the third-party software, including Clam/AV and Stata.

Ingest Space
^^^^^^^^^^^^

1. Create a directory for the ingest space
2. Map the ingest space as a drive on the web application machine,
   such as :file:`i:\\`
3. Map the ingest space as a drive on the curation service machine,
   such as :file:`i:\\`
4. Give the ``LocalService`` account, or the user account that will
   run the curator service, full access to the storage location.
5. Give the ``LocalService`` account, or the user account that will
   run the curator web application, full access to the storage
   location.
6. Give the ``LocalService`` account, or the user account that will
   run the clamav service, full access to the storage location.

Processing Space
^^^^^^^^^^^^^^^^

1. Create a directory for the processing space
2. Map the processing space as a drive on the web application machine,
   such as :file:`p:\\`
3. Map the processing space as a drive on the curation service
   machine, such as :file:`p:\\`
4. Give the ``LocalService`` account, or the user account that will
   run the curator service, full access to the storage location.
5. Give the ``LocalService`` account, or the user account that will
   run the curator web application, full access to the storage
   location.

Archive Space
^^^^^^^^^^^^^

1. Create a directory for the archiving space/transfer space
2. Map the archiving space as a drive on the web application machine,
   such as :file:`r:\\`
3. Map the archiving space as a drive on the curation service machine,
   such as :file:`r:\\`
4. Give the ``LocalService`` account, or the user account that will
   run the curator service, full access to the storage location.
5. Give the ``LocalService`` account, or the user account that will
   run the curator web app, full access to the storage location.
