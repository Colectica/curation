Upgrade
========

If you already have an installation of the Colectica Curation Tools
and would like to upgrade to a newer version, follow these
instructions.

.. seealso::

   For detailed instructions on first-time installation, see
   :doc:`deployment`.

#. Download and extract the latest
   :file:`ColecticaCurationPackage-{version}.zip` file.

#. Stop the Curation Service.

   .. image:: stop-curation-service.png

#. Copy the new :file:`ColecticaCurationWeb-{version}` directory over
   your existing :file:`ColecticaCurationWeb` deployment directory,
   overwriting the existing files.

   Do not delete your existing directory; your existing configuration
   files will stay in place.

#. Copy the new :file:`ColecticaCurationService-{version}` directory
   over your existing :file:`ColecticaCurationService` deployment
   directory, overwriting the existing files.

   Do not delete your existing directory; your existing configuration
   files will stay in place.

#. Restart the Curation Service.

#. Using your browser, test to ensure you have access to the curation
   web application.


Special Instructions When Upgrading to 0.9.*
-------------------------------------------------

#. After performing the steps above, navigate to the
   :file:`ColecticaCurationService` deployment directory.

#. Delete all :file:`Algenta.*.dll` files.

Special Instructions When Upgrading from 0.5.241
-------------------------------------------------

In the Windows Server Manager's Add Roles and Services Wizard, on the
:guilabel:`Server Roles` page, ensure the following is checked:
:guilabel:`Web Server` - :guilabel:`Web Server` - :guilabel:`Common
HTTP Features` - :guilabel:`Static Content`.
