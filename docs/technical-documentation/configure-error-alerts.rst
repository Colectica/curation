--------------------------
Configure Error Alerts
--------------------------

1. Open the :file:`Web.config` file in the curation web application's
   deployment directory.

2. Locate the ``<emlah>`` section and uncomment the ``<errorMail>``
   tag.

3. Fill in the attributes as appropriate.

   .. code-block:: xml

    <errorMail from="test@example.org"
           to="test@example.org"
           subject="Curation Application Exception"
           async="false"
           smtpPort="25"
           smtpServer="smtp.example.org"
           userName="***"
           password="***">
    </errorMail>
