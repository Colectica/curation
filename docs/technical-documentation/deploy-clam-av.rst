------------------------------------
Deploy ClamAV
------------------------------------

#. Create the :file:`c:\\clamav\\` directory.
#. Create the :file:`c:\\clamav\\db\\` directory.
#. Copy installation archive files from
   :file:`{PackageDirectory}\\clamav\\win64` into :file:`c:\\clamav`.
#. From the command prompt in :file:`c:\\clamav`.

    a. Download the latest virus definitions by running::

        freshclam

    b. Install the clamav service by running::

        clamd --install

    c. Install the freshclam service by running::

        freshclam --install

#. Open the Windows Services list.

    a. Set ``StartType`` to ``Automatic`` for both ``ClamWin Free Antivirus
       Scanner Service`` and ``ClamWin Free Antivirus Database Updater``.
    b. Start both the ``ClamWin Free Antivirus
       Scanner Service`` and ``ClamWin Free Antivirus Database Updater``.
