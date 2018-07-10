----------------------------
Create the Curation Database
----------------------------

Database Options
^^^^^^^^^^^^^^^^

Colectica Curation Service and Web Application are tested to work with
the following databases:

* Microsoft SQL Server 2008 R2 or higher
* Microsoft SQL Server Express 2008 R2 or higher

Create the Database
^^^^^^^^^^^^^^^^^^^

1. Connect to your SQL Server
2. Create a database named ``curation``
3. No SQL creation script is needed, as Entity Framework will create
   and manage the database.
4. Give the ``LocalService`` account, or the user account that will
   run the curation service, full access to the ``curation`` database.
5. Give the ``LocalService`` account, or the user account that will
   run the curation web application, full access to the ``curation``
   database.
