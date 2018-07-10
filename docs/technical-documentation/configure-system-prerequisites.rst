-------------------------------
Configure System Prerequisites
-------------------------------

1. In the Server Manager, choose :guilabel:`Add roles and features`

    .. image:: server-manager-choose-roles-and-features.png
   
2. On the :guilabel:`Features` page, check :guilabel:`.NET 3.5
   Features` and :guilabel:`.NET 4.5 Features`

   .. image:: roles-and-features-dot-net.png

   .. seealso::

      http://www.microsoft.com/en-us/download/details.aspx?id=42643

3. On the :guilabel:`Server Roles` page, check ``Web Server``

    .. image:: roles-and-features-web-server.png

4. Register ASP.NET with IIS

    a. For Windows Server 2012 computers, use the Server Manager's
       :guilabel:`Web Server Role` - :guilabel:`Role Services` page to
       select ``ASP.NET 3.5`` and ``ASP.NET 4.5``.
       
       .. image:: roles-and-features-register-iis.png
       
    b. Or, from the command line run::

            dism /online /enable-feature /all /featurename:IIS-ASPNET45

    c. For windows server 2008, run a command prompt as Administrator
       then launch
       ``C:\Windows\Microsoft.NET\Framework64\v4.0.30319>aspnet_regiis
       -i``

5. If the system has a firewall, ensure inbound ports 80 and 443 are
   allowed.
