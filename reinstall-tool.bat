@echo off
setlocal

pushd "%~dp0"

echo.
echo [reinstall-tool] Removing existing LiteGraphConsole global tool if present...
dotnet tool uninstall --global LiteGraphConsole >nul 2>nul

echo.
echo [reinstall-tool] Running fresh install...
call "%~dp0install-tool.bat"
set RESULT=%ERRORLEVEL%

popd
exit /b %RESULT%
