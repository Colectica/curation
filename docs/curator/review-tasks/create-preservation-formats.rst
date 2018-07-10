Create Preservation Formats
-----------------------------

The curation system automatically converts files of certain types to
preservation formats. The following conversions are supported.

===================  ===================
Proprietary Format   Preservation Format
===================  ===================
Stata Data (\*.dta)  CSV
R Data (\*.rda)      CSV
===================  ===================

The following file formats are considered preservation formats:

* CSV
* Text
* PDF

.. admonition:: TODO

   Cite a source and provide more details on what are acceptable
   preservation formats.

For files that are not in a preservation format and do not have a
built-in converter, the curator should manually create a file in a
preservation format and upload it to the catalog record.

#. For a proprietary file, select the :guilabel:`Create Preservation
   Format` task.

    .. image:: preservation-format-link.png

#. The review page will show the following information:

    a. Instructions indicating how to perform this review.

    b. Links to download the command file and any dependent files, such as
       the data file on which a command file acts.

    c. Links to download other files that are part of the catalog record.

    .. image:: preservation-format.png

#. Download the file.

#. Convert the file to an acceptable preservation format.

#. Upload the converted file.

#. Enter any desired comments and mark the review as complete.

    .. image:: review-task-accept.png
