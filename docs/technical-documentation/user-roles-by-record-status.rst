User Roles
===========

Each user of the curation system may perform certain actions on a
catalog record. Which actions are possible depends on the type of user
and the status of the catalog record. This table shows which actions
are available to which users at which times.

======================= ==========  ==========    ================  ================
..                      User Role
----------------------- ------------------------------------------  ----------------
Catalog Record Status   Depositor   Curator       Approver          Administrator
======================= ==========  ==========    ================  ================
New                     Edit                      Reject            Reject
Rejected                                          Un-reject         Un-reject      
Processing              View        Edit          View              Reject
Publication Requested   View        View          Approve, Reject   Approve, Reject
Publication Approved    View        View          View              View
Published               View        View          Unpublish         Unpublish
======================= ==========  ==========    ================  ================

The View permission is implied for all non-blank cells. It is only
listed explicitely when it is the only action available.
