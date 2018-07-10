************************************************************
Feature Overview
************************************************************

.. toctree::

Introduction
=================================

The Colectica Curation Tools allow data archives to leverage the DDI
standard for data documentation and to structure the curation
workflow. The platform combines several open source off-the-shelf
components with a new, web-based data pipeline application, and
enables a seamless framework for collecting, processing, archiving,
and publishing data. The platform enables open access to data
resources that have been fully reviewed and enhanced for long term
usability and analysis.
    
Ingest Studies and Files
=================================

Depositors use the web application to upload files and provide basic
information about their research.

Curation
=================================

Every catalog record is assigned a curator. The curator is responsible
for reviewing the record and all files. This review may include
several checks, which are assisted by the software.

Data Review
------------
* Find and remove confidential or identifiable information
* Find missing labels in data files and add appropriate metadata
* Identify potential data errors
* Confirm reported observation counts against the data

Code Review
------------
* Confirm that source code succesfully builds and executes
* Confirm that source code replicates reported results

Publication and Archiving
=================================

Once curation is complete and a catalog record is approved for
publication, the system automatically performs several actions to aid
in preservation and publication.

* Create preservation formats for proprietary data files.
* Request persistent identifiers from the configured Handle service
  and store them with the metadata.
* Create checksums for each file and store them with the metadata.
* Create DDI metadata for all files and the catalog record itself.
* Create an archive package and place it in the configured archive
  ingest location.
* Mark the catalog record as published.
