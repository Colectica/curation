set zip="c:\Program Files\7-Zip\7z.exe"
set msbuild="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\16.0\Bin\MSBuild.exe"

set WORKSPACE=c:\svn\curation
IF "%computername%"=="MARGOT" (
  set WORKSPACE=c:\jenkins\workspace\curation
)

REM Set the build number in the application.
IF "%computername%"=="MARGOT" (
  powershell -Command "(gc %WORKSPACE%\src\Colectica.Curation.Data\RevisionInfo.cs) -replace 'LOCAL_BUILD', $Env:BUILD_NUMBER | Out-File %WORKSPACE%\src\Colectica.Curation.Data\RevisionInfo.cs"
  powershell -Command "(gc %WORKSPACE%\src\Colectica.Curation.Data\RevisionNumber.txt) -replace 'LOCAL_BUILD', $Env:BUILD_NUMBER | Out-File %WORKSPACE%\src\Colectica.Curation.Data\RevisionNumber.txt"
)

PUSHD build

REM Clear any existing output.
rmdir /Q /S ..\dist
mkdir ..\dist

REM Restore nuget packages
PUSHD ..\src
.nuget\nuget.exe restore ColecticaCurationTools-WithColecticaSDK.sln
POPD

REM Build the WebDeploy packages.
%msbuild% ..\src\Colectica.Curation.Web\Colectica.Curation.Web.WithDdi.csproj  /P:Configuration=Release /P:Platform=AnyCPU /P:DeployOnBuild=true /p:VisualStudioVersion=12.0 /P:PublishProfile=FileBundle /P:SolutionDir=%WORKSPACE%\src\
if %errorlevel% neq 0 exit /b %errorlevel%

REM Include the version number in the file names.
REM set /p revisionNumber= <"..\src\Colectica.Curation.Data\RevisionNumber.txt"
set revisionNumber=1.0.0.%BUILD_NUMBER%
echo Revision number is %revisionNumber%

mkdir ..\dist\ColecticaCurationPackage-%revisionNumber%
set serviceDir=..\dist\ColecticaCurationPackage-%revisionNumber%\ColecticaCurationService-%revisionNumber%
set webDir=..\dist\ColecticaCurationWeb-%revisionNumber%

ren ..\dist\ColecticaCurationWeb ColecticaCurationWeb-%revisionNumber%

REM Copy service binaries to the dist/ folder.
xcopy ..\src\Colectica.Curation.Service\bin\Release %serviceDir% /i /s /y

REM TODO Rename config files to end in .dist
del %serviceDir%\Colectica.Curation.Service.vhost.exe.config
REM ren %serviceDir%\Colectica.Curation.Service.exe.config Colectica.Curation.Service.exe.config.dist
REM ren %serviceDir%\log4net.config log4net.config.dist
ren %serviceDir%\ConnectionStrings.config ConnectionStrings.config.dist
ren %serviceDir%\log4net.config log4net.config.dist

ren %webDir%\ConnectionStrings.config ConnectionStrings.config.dist
ren %webDir%\Web.config Web.config.dist
ren %webDir%\log4net.config log4net.config.dist

REM Copy spss-redist files to the web binary directory
xcopy /e /y ..\src\Colectica.Curation.Web\bin\spss-redist %webDir%\bin\spss-dist\

REM Copy the curation addins to the Web distribution directory
xcopy %serviceDir%\CurationAddins %webDir%\CurationAddins /i /s /y

REM Move the web deploy directory under the main package directory.
xcopy %webDir% ..\dist\ColecticaCurationPackage-%revisionNumber%\ColecticaCurationWeb-%revisionNumber% /i /s /y

REM Copy CurationAddins into the web distribution directory
xcopy %serviceDir%\CurationAddins %webDir%\CurationAddins /i /s /y

REM Copy ClamAV files
set clamDir=..\dist\ColecticaCurationPackage-%revisionNumber%\clamav
mkdir %clamDir%
xcopy ..\src\clamav %clamDir% /i /e /y
del %clamDir%\win64\EICAR-testfile.txt

REM ZIP everything
%zip% a ..\dist\ColecticaCurationPackage-%revisionNumber%.zip ..\dist\ColecticaCurationPackage-%revisionNumber%

REM Copy to the output directory
mkdir ..\dist\artifacts
move ..\dist\ColecticaCurationPackage-%revisionNumber%.zip ..\dist\artifacts\

POPD
