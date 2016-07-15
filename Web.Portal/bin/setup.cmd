REM   Attempt to set the execution policy by using PowerShell version 2.0 syntax.
   PowerShell -Command "Set-ExecutionPolicy Unrestricted" >> "c:\StartupLog.txt" 2>&1
   IF EXIST %CD%\PSScripts\ServiceStartup.ps1 PowerShell .\PSScripts\ServiceStartup.ps1 -EnvironmentName %1 -ServerRole %2 >> "c:\StartupLog2.txt" 2>&1	
   IF NOT EXIST %CD%\PSScripts\ServiceStartup.ps1 PowerShell  PowerShell ..\PSScripts\ServiceStartup.ps1 -EnvironmentName %1  -ServerRole %2 >> "c:\StartupLog3.txt" 2>&1
REM   If an error occurred, return the errorlevel.
EXIT /B %errorlevel%
