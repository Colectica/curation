Update Data Files from Metadata
================================

If you change the metadata of a Stata file while :doc:`performing
review tasks <perform-review-tasks>`, the curation tools can
automatically update the metadata in the Stata data file
(.dta). Supported updates include changes to:

* Variable names
* Variable labels
* Data types
* Value labels

1. Navigate to the :guilabel:`Status` page of a file that has pending
   metadata updates.

   .. image:: file-with-metadata-updates.png


2. Click the :guilabel:`Apply Pending Metadata Updates` button.

   .. image:: apply-pending-metadata-updates-button.png

   If a file has no pending metadata updates, the :guilabel:`Apply
   Pending Metadata Updates` button will not be displayed.

3. The curation tools will launch a background task to update the
   Stata data file.

   .. image:: applying-pending-metadata-updates.png

4. When complete, the Stata file will contain the updated metadata.
   The curation tools track this change to the file just like all
   other changes.
