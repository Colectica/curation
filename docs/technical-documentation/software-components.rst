Software Components
===================

The Colectica Curation Tools consist of two open source tools: the
Curation Web Application and the Curation Service. These two tools
serve as glue to bring together several off-the-shelf components to
provide a rich curation environment.

The following software components are used as part of the Colectica Curation Tools.

========================  ===========  =========================================  =============
Software                  License      Company                                    Required?
========================  ===========  =========================================  =============
Curation Web Application  Open Source  Colectica                                  Yes
Curation Service          Open Source  Colectica                                  Yes
Colectica Repository      Commercial   Colectica                                  No
Colectica Portal          Commercial   Colectica                                  No
Microsoft SQL Server      Commercial   Microsoft Corporation                      Yes
Stat/Transfer             Commercial   Circle Systems, Inc.                       No
ClamAV                    GPL          Cisco Systems, Inc.                        No
R                         GPL          R Core Team                                No
========================  ===========  =========================================  =============

The functionality provided by each of these components is described below.

-------------------------
Curation Web Application
-------------------------

* The Curation Web Application allows Depositors, Curators, and
  Administrators to submit, review, process, and publish data.

.. note::

   The Curation Web Application will be released as an open source project
   after further testing and packaging.

-------------------------
Curation Service
-------------------------

* The Curation Service performs long-running and automated tasks.
* These tasks are triggered by users interacting with the Curation Web
  Application or by other state changes.

.. note::

   The Curation Service will be released as an open source project
   after further testing and packaging.

-------------------------
Colectica Repository
-------------------------

* Colectica Repository stores all metadata in DDI Lifecycle format.
* Stores variable level information
* Automatically extracts variable level information from SPSS, Stata, CSV, and Excel files
* Provides functionality for web-based variable browsing and editing

-------------------
Colectica Portal
-------------------

* Colectica Portal allows users to browse and search published DDI metadata on the Web.

-----------------------------------------------------------------
Microsoft SQL Server or Microsoft SQL Server Express
-----------------------------------------------------------------

* Colectica Curation Tools uses Microsoft SQL Server as its database.

.. seealso::

   See
   http://www.microsoft.com/en-us/server-cloud/products/sql-server/
   for information about Microsoft SQL Server.

-------------------
ClamAV 
-------------------

* ClamAV is an open source antivirus engine for detecting trojans,
  viruses, malware & other malicious threats.
* Colectica Curation Tools uses ClamAV to scan deposited files.

.. seealso::

   See http://www.clamav.net/ for more information about ClamAV.

-------------------
Stat/Transfer
-------------------

* Stat/Transfer is used to convert \*.dta, \*.sav, and \*.rdata files to CSV 
  files. This happens during the publication process after publication is 
  approved, and before the archive package is created.

-------------------
R
-------------------

* R is a free software environment for statistical computing and
  graphics.
* Colectica Curation Tools use the R software to extract
  variable-level metadata from RData files.

.. seealso::

   See http://www.r-project.org/ for more information about R.

