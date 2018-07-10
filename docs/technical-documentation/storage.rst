Storage
========

The curation system uses two databases and three file storage
locations. Each of the file storage locations holds files at certain
stages of the curation pipeline.

------------
Databases
------------

Curation Database
^^^^^^^^^^^^^^^^^

The Curator Database stores state information, user information, and
all other information necessary for the curation system to
operate. The database runs on Microsoft SQL Server.

Colectica Repository Database
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The Colectica Repository database is used by Colectica Repository to store
metadata in DDI format.

---------------------
File Storage Areas
---------------------

Ingest File Storage
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The Ingest File Storage holds newly uploaded files until they are approved for
processing. This space is a configurable directory on a file system.

Processing File Storage
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The Processing File Storage holds files after they are approved for
processing. This space is a configurable directory on a file system.

Archival File Storage
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The Archival File Storage holds files after they are reviewed, finalized, and
pushed to the space. This space is a configurable directory on a file
system. Files in this location can be ingested into Fedora Commons or another
archival system.
