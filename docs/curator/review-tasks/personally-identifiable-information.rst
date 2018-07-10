Check for Personally-Identifiable Information
----------------------------------------------

The curator should identify any variables that can directly or
indirectly identify subjects.

.. seealso:: 

   For information about confidentiality and disclosure risk, see
   https://www.icpsr.umich.edu/icpsrweb/content/datamanagement/confidentiality/

#. For a data file, select the :guilabel:`Check
   Personally-Identifiable` Information task.

   .. image:: pii-link.png

#. The review page will show the following information:

    a. Instructions indicating how to perform this review.

    b. A list of all variables.

    c. A link to download the data file for manual modification.

    .. image:: pii.png

#. If you suspect the file may contain personally-identifiable
   information, take the following steps.

     a. Download the data file
     b. Remove or anonymize the appropriate information.
     c. Upload the new version of the file.

    .. note:: 

        Removing PII from the data file involves either using statistical
        software (e.g., Stata, R) to edit or write new code, or running a
        program/script that deletes or otherwise transforms these variables
        and writes out a new revised version of the data file, and adding the
        resulting data file to the catalog record (as well as any new code
        file).

#. You should always manually review all variables to verify that no
   potentially personally-identifiable information is present.

#. Once you are satisfied that the file does not contain
   personally-identifiable information, enter any desired
   comments and marks the review as complete.

   .. image:: review-task-accept.png

