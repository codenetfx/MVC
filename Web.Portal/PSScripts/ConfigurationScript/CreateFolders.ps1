<#
.SYNOPSIS
	Creates empty directories used by the configuration and deployment scripts

.PARAMETER ServerRole
	Type of server (Portal, Services SPWFE, SPAPP, SQL, AD)
#>
param
(
    [Parameter(Mandatory=$false)][String]$ServerRole,
    [Parameter(Mandatory=$false)][String]$EnvironmentName,
    [Parameter(Mandatory=$false)][string]$GlobalConfigurationFileName=$null,
	[Parameter(Mandatory=$false)][String]$XMLDirectory
)

Set-StrictMode -Version 2

#Region External Functions
	#Since, this is PowerShell v2, we can't use $PSScriptRoot
	$path = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
	. (Join-Path -Path $path -ChildPath "Common\ConfigurationScriptFunctions.ps1")
#EndRegion External Functions

function InServerRole ([string]$ServerRole, [string[]]$ServerRoleArray)
{
    foreach ($Role in $ServerRoleArray)
    {
        if ($ServerRole -eq $Role)
        {
            return $true
        }
    }

    #if we got here, we didn't find a match.
    return $false
}

function CreateBaseFolders([string[]]$BaseDirectories)
{
	foreach ($Directory in $BaseDirectories)
	{
		New-Item -Path $Directory -type directory -Force -ErrorAction SilentlyContinue
	}
}

function Main (
[Parameter(Mandatory=$false)][String]$ServerRole,
[Parameter(Mandatory=$false)][String]$EnvironmentName,
[Parameter(Mandatory=$false)][String]$GlobalConfigurationFileName,
[Parameter(Mandatory=$false)][String]$XMLDirectory
)
{

	$EnvironmentConfigurationFileName=$null
	$ServerName = $env:computername

    $GlobalConfigurationFileName = Ensure-EnvironmentFile $GlobalConfigurationFileName
    $GlobalConfiguration = Get-GlobalConfiguration -InputFile $GlobalConfigurationFileName
    Write-Debug "Calling Ensure-EnvironmentName"
    $EnvironmentName = Ensure-EnvironmentName -EnvironmentName $EnvironmentName -ServerName $ServerName -EnvironmentConfigurationFiles $GlobalConfiguration.EnvironmentConfigurationFiles
    Write-Debug "Calling Ensure-EnvironmentConfigurationFileName"
    $EnvironmentConfigurationFileName = Ensure-EnvironmentConfigurationFileName $EnvironmentConfigurationFileName $EnvironmentName $ServerName $GlobalConfiguration.EnvironmentConfigurationFiles
    Write-Debug "Calling Get-EnvironmentConfiguration"
    $EnvironmentConfiguration = Get-EnvironmentConfiguration -EnvironmentConfigurationFileName $EnvironmentConfigurationFileName -EnvironmentName $EnvironmentName -XMLDirectory $XMLDirectory

    $ServerRole = Ensure-DeploymentTier -DeploymentTier $ServerRole -ServerName $ServerName -EnvironmentConfiguration $EnvironmentConfiguration
	Write-Host "`$ServerRole:  $ServerRole"

	[string]$deploypath = $GlobalConfiguration.rootDestPath
	[string]$buildspath = $GlobalConfiguration.rootSrcPath
	[string]$SoftwareDirectory = $GlobalConfiguration.SoftwareDirectory
	[string]$DeplomentsScriptsDirectory = $GlobalConfiguration.DeplomentsScriptsDirectory
	[string]$DeploymentLogsDirectory = $GlobalConfiguration.DeploymentLogsDirectory
	[string]$EmailTmpDirectory = $GlobalConfiguration.EmailTmpDirectory
	[string]$EntLibLoggingInstallDirectory	= $GlobalConfiguration.EntLibLoggingInstallDirectory #Enterprise Library Logging Install location
	
	if (InServerRole -ServerRole $ServerRole -ServerRoleArray @("AD", "Portal", "Services", "SPAPP", "SPWFE", "SQL") )
	{
		$BaseDirectories = @($GlobalConfiguration.SoftwareDirectory, $GlobalConfiguration.ConfigurationScriptsDirectory)
		CreateBaseFolders -BaseDirectories $BaseDirectories
	}
	
	if ($ServerRole -eq "AD")
	{
		New-Item -Path $buildspath                 -type directory -Force -ErrorAction SilentlyContinue
		New-Item -Path $DeplomentsScriptsDirectory -type directory -Force -ErrorAction SilentlyContinue
		New-Item -Path $DeploymentLogsDirectory    -type directory -Force -ErrorAction SilentlyContinue
		New-Item -Path $SoftwareDirectory		   -type directory -Force -ErrorAction SilentlyContinue

		#Unzip -SourceDirectory $SoftwareDirectory -DestinationDirectory $GlobalConfiguration.ConfigurationScriptsDirectory -ZipFile $ZipFile 
		
	}

	if ($ServerRole -eq "Portal")
	{
		#New-Item -Path $EntLibLoggingInstallDirectory -type directory -Force -ErrorAction SilentlyContinue
	}

	if ($ServerRole -eq "Services")
	{
		New-Item -Path $EmailTmpDirectory -type directory -Force -ErrorAction SilentlyContinue
		#New-Item -Path $EntLibLoggingInstallDirectory       -type directory -Force -ErrorAction SilentlyContinue
	}

	if ($ServerRole -eq "SPWFE")
	{
		CreateBaseFolders -BaseDirectories $BaseDirectories
	}

	if ($ServerRole -eq "SPAPP")
	{
		CreateBaseFolders -BaseDirectories $BaseDirectories
	}

	if ($ServerRole -eq "SQL")
	{
		CreateBaseFolders - BaseDirectories $BaseDirectories
	}

}

Main -ServerRole $ServerRole -EnvironmentName $EnvironmentName -GlobalConfigurationFileName $GlobalConfigurationFileName -XMLDirectory $XMLDirectory