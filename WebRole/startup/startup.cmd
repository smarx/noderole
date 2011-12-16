cd /d "%~dp0"

REM Download everything
powershell -c "set-executionpolicy unrestricted"
powershell .\downloadstuff.ps1

REM Install IIS node
start /w vcredist_x64.exe /q
start /w msiexec /l* nodelog.txt /qn /i node-v0.6.6.msi
start /w msiexec /l* iislog.txt /qn /i iisnode-full-iis7-v0.1.12-x64.msi

REM Get rid of IIS idle timeout
%windir%\system32\inetsrv\appcmd set config -section:applicationPools -applicationPoolDefaults.processModel.idleTimeout:00:00:00

REM Install Git
7za x PortableGit-1.7.6-preview20110709.7z -y -o"%GITPATH%"
echo y| cacls "%GITPATH%" /grant everyone:f /t

REM Ensure app ACLs
echo y| cacls "%APPPATH%" /grant everyone:f /t

exit /b 0