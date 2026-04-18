@echo off
setlocal

pushd "%~dp0"

echo.
echo [install-tool] Building LiteGraph solution...
dotnet build src/LiteGraph.sln
if errorlevel 1 (
    popd
    exit /b 1
)

echo.
echo [install-tool] Packing LiteGraphConsole...
if not exist ".\src\nupkg" mkdir ".\src\nupkg"
dotnet pack src/LiteGraphConsole/LiteGraphConsole.csproj -o .\src\nupkg
if errorlevel 1 (
    popd
    exit /b 1
)

echo.
echo [install-tool] Installing LiteGraphConsole as global tool 'lg'...
dotnet tool install --global --add-source .\src\nupkg LiteGraphConsole

echo.
echo [install-tool] Completed.
popd
exit /b %ERRORLEVEL%
