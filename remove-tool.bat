@echo off
setlocal

echo.
echo [remove-tool] Uninstalling LiteGraphConsole global tool if present...
dotnet tool uninstall --global LiteGraphConsole >nul 2>nul

echo.
echo [remove-tool] Completed.
exit /b 0
