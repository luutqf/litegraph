@echo off
setlocal

cd /d "%~dp0"

set "FRAMEWORK=%~1"
if "%FRAMEWORK%"=="" set "FRAMEWORK=net10.0"

echo Running Test.Automated (%FRAMEWORK%)...
dotnet run --framework %FRAMEWORK% --project src\Test.Automated\Test.Automated.csproj
if errorlevel 1 exit /b %errorlevel%

echo Running Test.Xunit (%FRAMEWORK%)...
dotnet test --framework %FRAMEWORK% src\Test.Xunit\Test.Xunit.csproj
if errorlevel 1 exit /b %errorlevel%

echo Running Test.Nunit (%FRAMEWORK%)...
dotnet test --framework %FRAMEWORK% src\Test.Nunit\Test.Nunit.csproj
if errorlevel 1 exit /b %errorlevel%
