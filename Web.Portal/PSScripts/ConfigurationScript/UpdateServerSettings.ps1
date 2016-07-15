<#
.SYNOPSIS
	The functions in this file remove most of the annoying out of the box windows features.

.DESCRIPTION
 - Disable User Access Control (UAC)
 - Disable IE Enhanced Security Configuration(ESC)
 - Make File extensions visile
 - Make "normal" hidden files visible
 - Don't display server manager at start (it's the first icon in the task bar for heaven's sake)
#>
param(
    [Parameter(Mandatory=$false)][string]$EnvironmentName,
	[Parameter(Mandatory=$false)]$ServerRole,
    [Parameter(Mandatory=$false)][string]$GlobalConfigurationFileName,
	[Parameter(Mandatory=$false)][String]$XMLDirectory
)

Set-StrictMode -Version 2
#Region External Functions
	. "$PSScriptRoot\Common\ConfigurationScriptFunctions.ps1"
#EndRegion External Functions

$restartRequired = $false;

$ServerName = $env:computername
$EnvironmentConfigurationFileName=$null
	
$GlobalConfigurationFileName = Ensure-EnvironmentFile $GlobalConfigurationFileName
$GlobalConfiguration = Get-GlobalConfiguration -InputFile $GlobalConfigurationFileName

$EnvironmentName = Ensure-EnvironmentName -EnvironmentName $EnvironmentName -ServerName $ServerName -EnvironmentConfigurationFiles $GlobalConfiguration.EnvironmentConfigurationFiles

$EnvironmentConfigurationFileName = Ensure-EnvironmentConfigurationFileName $EnvironmentConfigurationFileName $EnvironmentName $ServerName $GlobalConfiguration.EnvironmentConfigurationFiles
$EnvironmentConfiguration = Get-EnvironmentConfiguration -EnvironmentConfigurationFileName $EnvironmentConfigurationFileName -EnvironmentName $EnvironmentName -XMLDirectory $XMLDirectory

$ServerRole = Ensure-DeploymentTier -DeploymentTier $ServerRole -ServerName $ServerName -EnvironmentConfiguration $EnvironmentConfiguration


#region 1.UpdateServerSettings

# the functions in this file remove most of the annoying out of the box windows features.
# - Disable User Access Control (UAC)
# - Disable IE Enhanced Security Configuration(ESC)
# - Make File extensions visible
# - Make "normal" hidden files visible
# - Don't display server manager at start (it's the first icon in the task bar for heaven's sake)

# - just execute the file, all functions are called at the end.
function Disable-InternetExplorerESC {
	$AdminKey = "HKLM:\SOFTWARE\Microsoft\Active Setup\Installed Components\{A509B1A7-37EF-4b3f-8CFC-4F3A74704073}"
	$UserKey =  "HKLM:\SOFTWARE\Microsoft\Active Setup\Installed Components\{A509B1A8-37EF-4b3f-8CFC-4F3A74704073}"
	Set-ItemProperty -Path $AdminKey -Name "IsInstalled" -Value 0
	Set-ItemProperty -Path $UserKey -Name "IsInstalled" -Value 0
	Stop-Process -Name Explorer
	Write-Host "IE Enhanced Security Configuration (ESC) has been disabled." -ForegroundColor Green
}
function Enable-InternetExplorerESC {
	$AdminKey = "HKLM:\SOFTWARE\Microsoft\Active Setup\Installed Components\{A509B1A7-37EF-4b3f-8CFC-4F3A74704073}"
	$UserKey = "HKLM:\SOFTWARE\Microsoft\Active Setup\Installed Components\{A509B1A8-37EF-4b3f-8CFC-4F3A74704073}"
	Set-ItemProperty -Path $AdminKey -Name "IsInstalled" -Value 1
	Set-ItemProperty -Path $UserKey -Name "IsInstalled" -Value 1
	Stop-Process -Name Explorer
	Write-Host "IE Enhanced Security Configuration (ESC) has been enabled." -ForegroundColor Green
}
function Disable-UserAccessControl {
	Set-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System" -Name "ConsentPromptBehaviorAdmin" -Value 00000000
	Set-ItemProperty -Path registry::HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Policies\System -Name EnableLUA -Value 0  
	Write-Host "User Access Control (UAC) has been disabled." -ForegroundColor Green 
}
function Disable-ShutdownEventTracker	{
    if ((Test-Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\Reliability") -eq $false)
    {
        New-Item -Path "HKLM:\SOFTWARE\Policies\Microsoft\Windows NT" -Name Reliability
        New-ItemProperty "HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\Reliability" -Name "ShutdownReasonOn" -Value 00000000
    }
	Set-ItemProperty "HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\Reliability" -Name "ShutdownReasonOn" -Value 00000000
	Write-Host "Shutdown Event Tracker has been disabled." -ForegroundColor Green 
}

function Set-FileView{
	Set-ItemProperty "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced" -Name "HideFileExt" -Value 00000000
	Write-Host "Showing File Extensions in Windows Explorer." -ForegroundColor Green 
	Set-ItemProperty "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced" -Name "Hidden" -Value 00000001
	Write-Host "Showing Hidden Files in Windows Explorer." -ForegroundColor Green 
}

function Disable-ShowingServerManagerAtLaunch{
	Set-ItemProperty "HKLM:\SOFTWARE\Microsoft\ServerManager" -Name "DoNotOpenServerManagerAtLogon" -Value 00000001
	Write-Host "Hiding Server Manager at launch." -ForegroundColor Green 
}

Disable-InternetExplorerESC
Disable-UserAccessControl

if ($EnvironmentName -ne "Production")
{
	Disable-ShutdownEventTracker
}

Set-FileView
Disable-ShowingServerManagerAtLaunch

#endregion UpdateServerSettings