param(
    [Parameter(Mandatory=$false)][string]$ServerRole,
    [Parameter(Mandatory=$true)][string]$EnvironmentName,
    [Parameter(Mandatory=$false)][string]$GlobalConfigurationFileName=$null
)

Set-StrictMode -Version 2

#$ErrorActionPreference = "Inquire"
$ErrorActionPreference = "Inquire"

#Region External Functions
	. "$PSScriptRoot\ConfigurationScript\Common\ConfigurationScriptFunctions.ps1"
#EndRegion External Functions




function ConfigureMiddleTierServers([Parameter(Mandatory=$true)]$basePath, $ServerRole,  $EnvironmentName, $GlobalConfigurationFileName )
{

		Add-WindowsFeature NET-Framework-Features
	  . $basePath\AddWindowsFeatures.ps1 -ServerRole $ServerRole  -EnvironmentName $EnvironmentName -GlobalConfigurationFileName $GlobalConfigurationFileName -XMLDirectory  "$PSScriptRoot\ConfigurationScript"
	  . $basePath\NetworkDTC.ps1 -ServerRole $ServerRole  -EnvironmentName $EnvironmentName -GlobalConfigurationFileName $GlobalConfigurationFileName -XMLDirectory  "$PSScriptRoot\ConfigurationScript"
	  . $basePath\Addusers2requiredLocalGroups.ps1 -ServerRole $ServerRole  -EnvironmentName $EnvironmentName -GlobalConfigurationFileName $GlobalConfigurationFileName -XMLDirectory  "$PSScriptRoot\ConfigurationScript"
	  . $basePath\OpenFirewall.ps1 -ServerRole $ServerRole  -EnvironmentName $EnvironmentName -GlobalConfigurationFileName $GlobalConfigurationFileName -XMLDirectory  "$PSScriptRoot\ConfigurationScript"
	 # . $basePath\UpdateServerSettings.ps1  -ServerRole "Services"  -EnvironmentName $EnvironmentName -GlobalConfigurationFileName $GlobalConfigurationFileName -XMLDirectory  "$PSScriptRoot\ConfigurationScript"
      . $basePath\CreateFolders.ps1 -ServerRole $ServerRole  -EnvironmentName $EnvironmentName -GlobalConfigurationFileName $GlobalConfigurationFileName -XMLDirectory  "$PSScriptRoot\ConfigurationScript"
	  . $basePath\DownloadSoftwareFromAzureBlobStorage.ps1 -ServerRole $ServerRole  -EnvironmentName $EnvironmentName   -GlobalConfigurationFileName $GlobalConfigurationFileName -XMLDirectory  "$PSScriptRoot\ConfigurationScript"
      . $basePath\UnzipSoftware.ps1 -ServerRole $ServerRole  -EnvironmentName $EnvironmentName -GlobalConfigurationFileName $GlobalConfigurationFileName -XMLDirectory  "$PSScriptRoot\ConfigurationScript"
	  . $basePath\CreateSQLAliases.ps1 -ServerRole $ServerRole  -EnvironmentName $EnvironmentName -GlobalConfigurationFileName $GlobalConfigurationFileName -XMLDirectory  "$PSScriptRoot\ConfigurationScript"
	  . $basePath\SetupQueueDistributor.ps1 -ServerRole $ServerRole  -EnvironmentName $EnvironmentName -GlobalConfigurationFileName $GlobalConfigurationFileName -XMLDirectory  "$PSScriptRoot\ConfigurationScript"
	  . $basePath\AspNetStateService.ps1

}



function Main(
[Parameter(Mandatory=$false)][string]$ServerRole,
[Parameter(Mandatory=$true)][string]$EnvironmentName,
[Parameter(Mandatory=$false)][string]$GlobalConfigurationFileName
)
{

	$EnvironmentConfigurationFileName=$null
	$ServerName = $env:computername
    $basePath = "$PSScriptRoot\ConfigurationScript"
    $GlobalConfigurationFileName = Ensure-EnvironmentFile $GlobalConfigurationFileName -XMLDirectory  $basePath
    $GlobalConfiguration = Get-GlobalConfiguration -InputFile $GlobalConfigurationFileName
    $EnvironmentName = Ensure-EnvironmentName -EnvironmentName $EnvironmentName -ServerName $ServerName -EnvironmentConfigurationFiles $GlobalConfiguration.EnvironmentConfigurationFiles
    $EnvironmentConfigurationFileName = Ensure-EnvironmentConfigurationFileName $EnvironmentConfigurationFileName $EnvironmentName $ServerName $GlobalConfiguration.EnvironmentConfigurationFiles
    $EnvironmentConfiguration = Get-EnvironmentConfiguration -EnvironmentConfigurationFileName $EnvironmentConfigurationFileName -EnvironmentName $EnvironmentName -XMLDirectory  $basePath

    #$ServerRole = Ensure-DeploymentTier -DeploymentTier $ServerRole -ServerName $ServerName -EnvironmentConfiguration $EnvironmentConfiguration
    #$basePath = $GlobalConfiguration.ConfigurationScriptsDirectory

	ConfigureMiddleTierServers -basePath $basePath -ServerRole "Portal"  -EnvironmentName $EnvironmentName -GlobalConfigurationFileName $GlobalConfigurationFileName
}

Main -ServerRole $ServerRole -EnvironmentName $EnvironmentName -GlobalConfigurationFileName $GlobalConfigurationFileName

