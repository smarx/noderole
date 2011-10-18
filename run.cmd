@echo off
if "%ServiceHostingSDKInstallPath%" == "" (
    echo Can't see the ServiceHostingSDKInstallPath environment variable. Please run from a Windows Azure SDK command-line (run Program Files\Windows Azure SDK\^<version^>\bin\setenv.cmd^).
    GOTO :eof
)
csrun %~dp0NodeRole\bin\release\NodeRole.csx %~dp0NodeRole\ServiceConfiguration.Local.cscfg /launchBrowser