Extension Points
=================

The Colectica Curation Tools are highly extensible via addins. Addins
fall into two categories: Actions and Editors. Each type of extension
point is described below.

.. note::

   Technical documentation and samples for each type of extension
   point will be available in the future.

Action Addins
--------------

Create Catalog Record
    Called after a new record is created.
File Action
    Called when a new file is added to a catalog record.
Apply Metadata Updates
    Called when the user requests that metadata updates be applied to
    a file.
Submit for Curation
    Called after a depositor submits a record for curation.
Create Persistent Identifiers
    Called during catalog record finalization, to retrieve and set
    persistent identifiers.
Create Preservation Formats
    Called during catalog record finalization, to create safe
    preservation formats from proprietary file formats.
Finalize Metadata
    Called during catalog record finalization, to create standardized
    metadata.
Publish
    Called during catalog record finalization, to publish information
    to a dissemination or archival system.

Editor Addins
--------------

Catalog Record Editor
    Edit information about a catalog record.
Managed File Editor
    Edit information about a file (e.g., variables in a data file).
