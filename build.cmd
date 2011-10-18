@echo off
if "%VSINSTALLDIR%" == "" (
    echo Can't see the VSINSTALLDIR environment variable. Please run from a VS command-line (run vsvars32.bat^).
    GOTO :eof
)
msbuild /t:publish /p:configuration=release