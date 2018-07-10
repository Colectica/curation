REM @echo off

IF EXIST "C:\Program Files (x86)\TortoiseSVN\bin\SubWCRev.exe" GOTO win64
"C:\Program Files\TortoiseSVN\bin\SubWCRev.exe" ..\ RevisionInfo.cs.template RevisionInfo.cs
"C:\Program Files\TortoiseSVN\bin\SubWCRev.exe" ..\ RevisionNumber.template RevisionNumber.txt

GOTO end

:win64
"C:\Program Files (x86)\TortoiseSVN\bin\SubWCRev.exe" ..\ RevisionInfo.cs.template RevisionInfo.cs
"C:\Program Files (x86)\TortoiseSVN\bin\SubWCRev.exe" ..\ RevisionNumber.template RevisionNumber.txt

:end
