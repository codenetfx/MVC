<#
.SYNOPSIS
	Unzips software

.PARAMETER SourceDirectory
	Directory that contains the zip file

.PARAMETER ZipFileName
	The name of the zip file that contains SSMS

.PARAMETER DestinationDirectory
	The directory to install SSMS
#>
param
(
[Parameter(Mandatory=$false)][String]$SourceDirectory,
[Parameter(Mandatory=$false)][string]$DestinationDirectory,
[Parameter(Mandatory=$false)][string]$ServerRole,
[Parameter(Mandatory=$false)][string]$EnvironmentName,
[Parameter(Mandatory=$false)][string]$GlobalConfigurationFileName,
[Parameter(Mandatory=$false)][String]$XMLDirectory
)

Set-StrictMode -Version 2
#Region External Functions
	. "$PSScriptRoot\Common\ConfigurationScriptFunctions.ps1"
#EndRegion External Functions

function Main(
[Parameter(Mandatory=$false)][String]$SourceDirectory,
[Parameter(Mandatory=$false)][string]$DestinationDirectory,
[Parameter(Mandatory=$false)][string]$ServerRole,
[Parameter(Mandatory=$false)][string]$EnvironmentName,
[Parameter(Mandatory=$false)][string]$GlobalConfigurationFileName,
[Parameter(Mandatory=$false)][String]$XMLDirectory
)
{
	$EnvironmentConfigurationFileName=$null
	$ServerName = $env:computername

    $GlobalConfigurationFileName = Ensure-EnvironmentFile $GlobalConfigurationFileName
    $GlobalConfiguration = Get-GlobalConfiguration -InputFile $GlobalConfigurationFileName

    $EnvironmentName = Ensure-EnvironmentName -EnvironmentName $EnvironmentName -ServerName $ServerName -EnvironmentConfigurationFiles $GlobalConfiguration.EnvironmentConfigurationFiles

    $EnvironmentConfigurationFileName = Ensure-EnvironmentConfigurationFileName -EnvironmentConfigurationFileName $EnvironmentConfigurationFileName -EnvironmentName $EnvironmentName -ServerName $ServerName -EnvironmentConfigurationFiles $GlobalConfiguration.EnvironmentConfigurationFiles
    $EnvironmentConfiguration = Get-EnvironmentConfiguration -EnvironmentConfigurationFileName $EnvironmentConfigurationFileName -EnvironmentName $EnvironmentName -XMLDirectory $XMLDirectory

    $ServerRole = Ensure-DeploymentTier -DeploymentTier $ServerRole -ServerName $ServerName -EnvironmentConfiguration $EnvironmentConfiguration

	$SoftwareDirectory = $GlobalConfiguration.SoftwareDirectory
	$CIArtifactsDirectory = $GlobalConfiguration.rootSrcPath
	$ToolsPath = $GlobalConfiguration.ToolsPath
	$EntLibLoggingInstallDirectory = $GlobalConfiguration.EntLibLoggingInstallDirectory
	$UtilsDirectory = $GlobalConfiguration.UtilsDirectory
	
	if ($ServerRole -eq "AD")
	{
		Unzip -SourceArchiveFileName (Join-Path $SoftwareDirectory "MSBuildTools.zip") -DestinationDirectoryName $ToolsPath #MS Build Tools used by DBDeploy
	}
	
	if ($ServerRole -eq "SQL")
	{
		#Unzip -SourceArchiveFileName (Join-Path $SoftwareDirectory "en_sql_server_2012_enterprise_edition_with_sp1_x64_dvd_1227976.zip") -DestinationDirectoryName $SoftwareDirectory #SQL Server 2012 RTM Installation
	
	}
	
	if ($ServerRole -eq "Portal")
	{
		Unzip -SourceArchiveFileName (Join-Path $SoftwareDirectory "EnterpriseLibraryLogging.zip") -DestinationDirectoryName $EntLibLoggingInstallDirectory #EntLib Logging
	}
	
	if ($ServerRole -eq "Services")
	{
		Unzip -SourceArchiveFileName (Join-Path $SoftwareDirectory "EnterpriseLibraryLogging.zip") -DestinationDirectoryName $EntLibLoggingInstallDirectory #EntLib Logging
		Unzip -SourceArchiveFileName (Join-Path $SoftwareDirectory "Tools.zip") -DestinationDirectoryName $UtilsDirectory #Debugging Utilities/Tools
	}

	if ($ServerRole -in @("SPAPP", "SPWFE"))
	{
        $SharePointDirectory = (Join-Path $GlobalConfiguration.ConfigurationScriptsDirectory "SPAPP\SP\2013\SharePoint") #SharePoint and prerequisites will be place in the AutoSPInstaller directory tree.
 	    $SharePointUpdatesDirectory = (Join-Path $GlobalConfiguration.ConfigurationScriptsDirectory "SPAPP\SP\2013\Updates") #Non-Slipstreamed SharePoint Updates will be place in the AutoSPInstaller directory tree.
        $SharePointSlipStreamedUpdatesDirectory = (Join-Path $GlobalConfiguration.ConfigurationScriptsDirectory "SPAPP\SP\2013\SharePoint\Updates") #Slipstreamed SharePoint Updates will be place in the AutoSPInstaller directory tree.
        $SharePointPrerequisitesDirectory = (Join-Path $GlobalConfiguration.ConfigurationScriptsDirectory "SPAPP\SP\2013\SharePoint\PrerequisiteInstallerFiles") #SharePoint Prequisites Directory

		Unzip -SourceArchiveFileName (Join-Path $SoftwareDirectory "SharePoint2013_RTM.zip") -DestinationDirectoryName $SharePointDirectory #SharePoint 2013 Installation
        Unzip -SourceArchiveFileName (Join-Path $SoftwareDirectory "SP2013_Prerequisites.zip") -DestinationDirectoryName $SharePointPrerequisitesDirectory #SharePoint 2013 Prequisites that apply to all OS.

        #Unzip SharePoint prerequisites that are operating system dependant
        if ((Get-WmiObject  Win32_OperatingSystem).Version -eq "6.1.7601") # Win2008 R2 SP1
                {
                    Unzip -SourceArchiveFileName (Join-Path $SoftwareDirectory "SP2013_Prerequisites_Windows2008R2.zip") -DestinationDirectoryName $SharePointPrerequisitesDirectory
                }

        $QueryOS = Get-WmiObject Win32_OperatingSystem
        If ($QueryOS.Version.Contains("6.2") -or $QueryOS.Version.Contains("6.3")) # Windows 2012 or 2012 R2
	        {
                Unzip -SourceArchiveFileName (Join-Path $SoftwareDirectory "SP2013_Prerequisites_Windows2012.zip") -DestinationDirectoryName $SharePointPrerequisitesDirectory
            }

		Unzip -SourceArchiveFileName (Join-Path $SoftwareDirectory "SharePointUtils.zip") -DestinationDirectoryName $UtilsDirectory #Debugging Utilities/Tools
		Unzip -SourceArchiveFileName (Join-Path $SoftwareDirectory "Tools.zip") -DestinationDirectoryName $UtilsDirectory #Debugging Utilities/Tools
	}
	
	
	if ($ServerRole -eq "OnPrem")
	{
		Unzip -SourceArchiveFileName (Join-Path $SoftwareDirectory "EnterpriseLibraryLogging.zip") -DestinationDirectoryName $EntLibLoggingInstallDirectory #EntLib Logging
		Unzip -SourceArchiveFileName (Join-Path $SoftwareDirectory "Tools.zip") -DestinationDirectoryName $UtilsDirectory #Debugging Utilities/Tools

	}
}

Main -SourceDirectory $SourceDirectory -DestinationDirectory $DestinationDirectory -ServerRole $ServerRole -EnvironmentName $EnvironmentName -GlobalConfigurationFileName $GlobalConfigurationFileName -XMLDirectory $XMLDirectory
