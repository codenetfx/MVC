
Set-StrictMode -Version 2

#region Environment_Functions

<#
.SYNOPSIS
	Validates the server role

.PARAMETER ServerRole
	Type of server (Portal, Services SPWFE, SPAPP, SQL, AD)
	
.DESCRIPTION
	Validates whether a specified server roles is in the domain of valid values.
    Uses a foreach loop instead of using the -in operator to work with PowerShell 2.
#>
function ValidateServerRole([Parameter(Mandatory=$true)][String]$ServerRole)
{
	$serverRoles = @("AD","SQL", "SPWFE", "SPAPP", "Portal", "Services", "AllAzure", "OnPrem" )
    foreach ($Role in $serverRoles)
    {
        if ($Role -eq $serverRole)
        {
            return $true
        }
    }

	return $false
}

<#
.SYNOPSIS
	Gets the name of the environment in the environments.xml file
	the specified server belongs to.
	
.PARAMETER ServerRole

.PARAMETER ServerName

.PARAMETER ServerRoleEnvironmentConfigurations

#>
function Get-EnvironmentName(
[Parameter(Mandatory=$true)][String]$ServerName, 
[Parameter(Mandatory=$true)][object[]]$EnvironmentConfigurationFiles,
[Parameter(Mandatory=$false)][String]$ConfigurationXmlPath
)
{
    Write-Debug "In Get-EnvironmentName"
    if ($ConfigurationXmlPath)
    {
        $path = $ConfigurationXmlPath
    }
    else
    {
        $path = Get-ScriptDirectory
    }

    Write-Debug "Get-EnvironmentName() - Get-ScriptDirectory returned: $path"
    if ($PSVersionTable.PSVersion.Major -eq 3)
    {
        $path = Split-Path $path -Parent
    }

    $path = (Join-Path -Path $path -childPath "XML")

    foreach ($EnvironmentConfigurationFile in $EnvironmentConfigurationFiles)
    {

		$InputFile = (Join-Path -Path $path -ChildPath $EnvironmentConfigurationFile.FileName)
        Write-Debug "Input file: $InputFile"
        Write-Host "Checking configuration file:  $InputFile"
        $xmlInput = [xml] (get-content $InputFile)
        $EnvironmentConfigurations = $xmlInput.Configuration.Environments.Environment

        foreach ($EnvironmentConfiguration in $EnvironmentConfigurations)
        {
            $Environment = $EnvironmentConfiguration

            if ($Environment.PortalServers -ne $null)
            {
                foreach ($PortalServer in @($Environment.PortalServers.Split()))
                {
                    if ($ServerName -eq $PortalServer)
                    {
                        Write-Debug ("Found in " + $EnvironmentConfiguration.Name)
                        return $EnvironmentConfiguration.Name
                    }
                }
            }
        
            if ($Environment.MiddleTierServers -ne $null)
            {

                foreach ($MiddleTierServer in @($Environment.MiddleTierServers.Split()))
                {
                    if ($ServerName -eq $MiddleTierServer)
                    {
                        Write-Debug ("Found in " + $EnvironmentConfiguration.Name)
                        return $EnvironmentConfiguration.Name
                    }
                }
            }

            if ($Environment.SharePointServer -ne $null)
            {
                foreach ($SharePointServer in @($Environment.SharePointServer.Split()) )
                {
                    if ($ServerName -eq $SharePointServer)
                    {
                        Write-Debug ("Found in " + $EnvironmentConfiguration.Name)
                        return $EnvironmentConfiguration.Name
                    }
                }
            }

            if ($Environment.SharePointAPPs -ne $null)
            {
                foreach ($SharePointAPP in @($Environment.SharePointAPPs.Split()))
                {
                    if ($ServerName -eq $SharePointAPP)
                    {
                        Write-Debug ("Found in " + $EnvironmentConfiguration.Name)
                        return $EnvironmentConfiguration.Name
                    }
                }
            }

            if ($Environment.SharePointWFEs -ne $null)
            {
                foreach ($SharePointWFE in @($Environment.SharePointWFEs.Split()))
                {
                    if ($ServerName -eq $SharePointWFE)
                    {                   
                        Write-Debug ("Found in " + $EnvironmentConfiguration.Name)
                        return $EnvironmentConfiguration.Name
                    }
                }
            }

            if ($Environment.SqlServers -ne $null)
            {
                foreach ($SqlServer in $Environment.SqlServers.SQLServer)
                {
                    if ($ServerName -eq $SqlServer.Name)
                    {
                        Write-Debug ("Found in " + $EnvironmentConfiguration.Name)
                        return $EnvironmentConfiguration.Name
                    }
                }
            }

            if ($Environment.ActiveDirectoryServers -ne $null)
            {
                foreach ($ActiveDirectoryServer in @($Environment.ActiveDirectoryServers.Split()))
                {
                    if ($ServerName -eq $ActiveDirectoryServer)
                    {
                        Write-Debug ("Found in " + $EnvironmentConfiguration.Name)
                        return $EnvironmentConfiguration.Name
                    }
                }
            }

        } # foreach - EnvironmentConfigurations
    } # foreach - EnvironmentConfigurationFiles
}

<#
.SYNOPSIS
	Gets the Deployment Tier that the script is running on.
	
.PARAMETER ServerName

.PARAMETER ServerRoleEnvironmentConfiguration

.PARAMETER EnvironmentName

.PARAMETER EnvironmentConfigurations

.DESCRIPTION
	Valid Tiers are: Portal, Services, SPAPP, SPWFE, SQL, AD
	Assumes a server belongs to a single tier AND a server contains a single tier
#>
function Get-DeploymentTier([Parameter(Mandatory=$true)][String]$ServerName, [Parameter(Mandatory=$true)][object]$EnvironmentConfiguration)
{
        $Environment = $EnvironmentConfiguration

        if ($Environment.PortalServers -ne $null)
        {
            foreach ($PortalServer in @($Environment.PortalServers))
            {
                if ($ServerName -eq $PortalServer)
                {
                    Write-Debug "Server role is Portal"
                    return "Portal"
                }
            }
        }
        
        if ($Environment.MiddleTierServers -ne $null)
        {
            foreach ($MiddleTierServer in @($Environment.MiddleTierServers))
            {
                if ($ServerName -eq $MiddleTierServer)
                {
                    Write-Debug "Server role is Services"
                    return "Services"
                }
            }
        }

        if ($Environment.SharePointServer -ne $null)
        {
            foreach ($SharePointServer in @($Environment.SharePointServer))
            {
                if ($ServerName -eq $SharePointServer)
                {
                    Write-Debug "Server role is SPAPP"
                    return "SPAPP"
                }
            }
        }

        if ($Environment.SharePointAPPs -ne $null)
        {
            foreach ($SharePointAPP in @($Environment.SharePointAPPs))
            {
                if ($ServerName -eq $SharePointAPP)
                {
                    Write-Debug "Server role is SPAPP"
                    return "SPAPP"
                }
            }
        }

        if ($Environment.SharePointWFEs -ne $null)
        {
            foreach ($SharePointWFE in @($Environment.SharePointWFEs))
            {
                if ($ServerName -eq $SharePointWFE)
                {
                    Write-Debug "Server role is SPWFE"
                    return "SPWFE"
                }
            }
        }

        if ($Environment.SqlServers -ne $null)
        {
            foreach ($SqlServer in @($Environment.SqlServers))
            {
                if ($ServerName -eq $SqlServer.Name)
                {
                    Write-Debug "Server role is SQL"
                    return "SQL"
                }
            }
        }

        if ($Environment.ActiveDirectoryServers -ne $null)
        {
            foreach ($ActiveDirectoryServer in @($Environment.ActiveDirectoryServers))
            {
                if ($ServerName -eq $ActiveDirectoryServer)
                {
                    Write-Debug "Server role is AD"
                    return "AD"
                }
            }
        }

        Write-Debug "Server role not found."
}

<#
.SYNOPSIS Ensures a valid environment name.

.PARAMETER EnvironmentName

.PARAMETER ServerName

.PARAMETER EnvironmentName

.PARAMETER EnvironmentConfigurations

.DESCRIPTION
	If an environment name isn't provided, one is inferred from the computername and the envirment.xml file.
#>
function Ensure-EnvironmentName(
[Parameter(Mandatory=$false)][String]$EnvironmentName,
[Parameter(Mandatory=$true)][String]$ServerName, 
[Parameter(Mandatory=$true)][object[]]$EnvironmentConfigurationFiles,
[Parameter(Mandatory=$false)][String]$ConfigurationXmlPath
)
{
    Write-Debug "In Ensure-EnvironmentName"
	if ([string]::IsNullOrEmpty($EnvironmentName) -or ($EnvironmentName.Length -eq 0)) 
	{
		$EnvironmentName = Get-EnvironmentName -ServerName $ServerName -EnvironmentConfigurationFiles $EnvironmentConfigurationFiles -ConfigurationXmlPath $ConfigurationXmlPath
	}
	
	if ($EnvironmentName)
	{
		#Validate EnvironmentName
        [bool] $ValidEnvironmentNameFound = $true
        foreach ($EnvironmentConfigurationFile in $EnvironmentConfigurationFiles)
        {
              if ($EnvironmentName -eq $EnvironmentConfigurationFile.Name)
              {
                $ValidEnvironmentNameFound = $true
                continue;
              }
         }

		Write-Debug "`$EnvironmentName:  $EnvironmentName"
        if (!$ValidEnvironmentNameFound)		
        {
            $Message = "The EnvironmentName $EnvironmentName isn't valid" 
		    $RecommendedAction = "Make sure EnvironmentName is registered in the global environment file"
		    Write-Error -Message $Message -RecommendedAction $RecommendedAction
        }
	}
	else
	{
		$Message = "The server $ServerName doesn't belong to any known environment configurations" 
		$RecommendedAction = "Make sure server name is registered in the global environment file"
		Write-Error -Message $Message -RecommendedAction $RecommendedAction
        Exit -1
	}

	return $EnvironmentName
}


<#
.SYNOPSIS Retrieves XML directory location

.DESCRIPTION
	Temporary workaround to retrieve the XML directory location.
#>
function Get-XMLDirectory([Parameter(Mandatory=$false)][String]$XMLDirectory)
{
    #Temporary workaround to support different caller script locations returning different paths.
    if (![string]::IsNullOrEmpty($XMLDirectory))
    {
       return "$XMLDirectory\xml"
    }
    else
    {
         return "c:\apps\xml"
    }
    
}

<#
.SYNOPSIS Ensures a valid environment name.

.PARAMETER EnvironmentName

.PARAMETER ServerName

.PARAMETER EnvironmentName

.PARAMETER EnvironmentConfigurations

.DESCRIPTION
	If an environment configuration file name isn't provided, one is inferred from the computername or the EnvironmentName and the envirment.xml file.
#>
function Ensure-EnvironmentConfigurationFileName(
[Parameter(Mandatory=$false)][String]$EnvironmentConfigurationFileName,
[Parameter(Mandatory=$false)][String]$EnvironmentName,
[Parameter(Mandatory=$false)][String]$ServerName,
[Parameter(Mandatory=$true)][object[]]$EnvironmentConfigurationFiles,
[Parameter(Mandatory=$false)][String]$ConfigurationXmlPath,
[Parameter(Mandatory=$false)][String]$XMLDirectory
)
{
    Write-Debug "In Ensure-EnvironmentConfigurationFileName"
    # If the config file name was provided, then make sure it exists and return the same file name.
    if (![string]::IsNullOrEmpty($EnvironmentConfigurationFileName))
    {
        $path = Get-XMLDirectory -XMLDirectory $XMLDirectory

        Write-Debug ("Ensure-EnvironmentConfigurationFileName() - Path:  " + $path)
		$InputFile = (Join-Path -Path $path -ChildPath $EnvironmentConfigurationFileName)

        if (Test-Path $InputFile)
        {
            return $EnvironmentConfigurationFileName
        }
    }

    # If the environment name wasn't specified, go get it.
	if ([string]::IsNullOrEmpty($EnvironmentName) -or ($EnvironmentName.Length -eq 0)) 
	{
		$EnvironmentName = Get-EnvironmentName -ServerName $ServerName -EnvironmentConfigurationFiles $EnvironmentConfigurationFiles -ConfigurationXmlPath $ConfigurationXmlPath
	}
	
    # Use the environment name to figure out the environment configuration file name.
	if ($EnvironmentName)
	{
		#Validate EnvironmentName
        [string]$EnvironmentConfigurationFileName = $null
        foreach ($EnvironmentConfigurationFile in $EnvironmentConfigurationFiles)
        {
              if ($EnvironmentName -eq $EnvironmentConfigurationFile.Name)
              {
                $EnvironmentConfigurationFileName = $EnvironmentConfigurationFile.FileName
                continue;
              }
         }

		Write-Debug "`$EnvironmentName:  $EnvironmentName"
        # If we weren't able to figure out the EnvironmentName, we can't figure out the config file name.
        if ([String]::IsNullOrEmpty($EnvironmentConfigurationFileName))	
        {
            $Message = "The EnvironmentName $EnvironmentName isn't valid" 
		    $RecommendedAction = "Make sure EnvironmentName is registered in $EnvironmentConfigurationFileName"
		    Write-Error -Message $Message -RecommendedAction $RecommendedAction
        }
	}
	else
	{
		$Message = "The server $ServerName doesn't belong to any known environment configurations" 
		$RecommendedAction = "Make sure server name is registered in $EnvironmentConfigurationFileName"
		Write-Error -Message $Message -RecommendedAction $RecommendedAction
        Exit -1
	}

	return $EnvironmentConfigurationFileName
}

<#
.SYNOPSIS Ensures a valid deployment tier.

.PARAMETER DeploymentTier

.PARAMETER ServerName

.PARAMETER EnvironmentName

.PARAMETER EnvironmentConfigurations

.DESCRIPTION
	If deployment tier isn't provided, one is inferred from the computername and the envirment.xml file.
#>
function Ensure-DeploymentTier([Parameter(Mandatory=$false)][String]$DeploymentTier, [Parameter(Mandatory=$true)][String]$ServerName, [Parameter(Mandatory=$true)][object]$EnvironmentConfiguration)
{
	if ([String]::IsNullOrEmpty($DeploymentTier))
	{
		$DeploymentTier= Get-DeploymentTier -ServerName $ServerName -EnvironmentConfiguration $EnvironmentConfiguration
	}
	
	if (!(ValidateServerRole -ServerRole $DeploymentTier))
	{
		Write-Error "Invalid Server Role/Tier"
	}
	
	return $DeploymentTier
}


function Ensure-EnvironmentFile([Parameter(Mandatory=$false)][string]$InputFile, [Parameter(Mandatory=$false)][String]$XMLDirectory)
{
    if([string]::IsNullOrEmpty($InputFile))
    {
        $path = Get-XMLDirectory -XMLDirectory $XMLDirectory

        Write-Debug ("Ensure-EnvironmentFile() - Path:  " + $path)
		$InputFile = (Join-Path -Path $path -ChildPath "Environments-Global.xml")
    }

    if (!(Test-Path $InputFile))
    {
        Write-Host "Can't locate Input File $InputFile"
        Exit -1
	}
	
	return $InputFile
}
# This function assumes that the netbios domain name is part of the DNS domain name.
# In some environments this is not a valid assumption.
function Get-WindowsNtDomain()
{
		#(Get-WmiObject Win32_ComputerSystem).Domain.Replace(".aria.local", "")
		#$env:userdnsdomain
		$computer = Get-WmiObject -Class Win32_ComputerSystem
		$curDomain= ($computer).Domain
		$netbiosname = $curdomain.Substring(0,$curdomain.IndexOf("."))
		
		return $netbiosname
}
#endregion Environment_Functions

#region Build_Directory
function private:Get-NewestBuildNumberDirectory([Parameter(Mandatory=$true)][String]$ArtifactsDirectoryPath)
{
	Get-ChildItem -path $ArtifactsDirectoryPath -Exclude "Staging"| ? {$_.Attributes -eq "Directory"} | Sort-Object -Descending LastWriteTime | select -First 1
}

<#
.SYNOPSIS
	Gets the directory path to the newest build number.

.PARAMETER ArtifactsDirectoryPath
	The directy path that points to the CI artifacts directory where builds are located.
#>
function Get-NewestBuildNumberDirectoryPath([Parameter(Mandatory=$true)][String]$ArtifactsDirectoryPath)
{
	$Directory = GetNewestBuildNumberDirectory -ArtifactsDirectoryPath $ArtifactsDirectoryPath
    return $Directory.FullName
}

function Get-NewestBuildNumber([Parameter(Mandatory=$true)][String]$ArtifactsDirectoryPath)
{
    $NewestBuildNumberDirectory = GetNewestBuildNumberDirectory -ArtifactsDirectoryPath $ArtifactsDirectoryPath
    return $NewestBuildNumberDirectory.BaseName
}

function Ensure-BuildNumber([Parameter(Mandatory=$true)][String]$BuildNumber, [Parameter(Mandatory=$true)][String]$ArtifactsDirectoryPath)
{
	if ($BuildNumber -eq $null)
	{
		$BuildNumber = Get-NewestBuildNumber -ArtifactsDirectoryPath $ArtifactsDirectoryPath
	}
	
	if (!(Test-Path (Join-Path $ArtifactsDirectoryPath $BuildNumber)))
	{
		throw "Invalid Build Number."
	}
	
	return $BuildNumber
}

#endregion Build_Directory

#region Configuration_Data_Functions
 function Get-GlobalConfiguration([Parameter(Mandatory=$true)][string]$InputFile)
 {
    $xmlInput = [xml] (get-content $InputFile)

    $GlobalConfiguration = $xmlInput.Configuration.Global

    #Global Configuration Information from Environments-Global.xml
    [string]$rootSrcPath   = $GlobalConfiguration.rootSrcPath   	                  # The root directory where install binaries live
	[string]$rootDestPath  = $GlobalConfiguration.rootDestPath                        # Installed binaries are placed here on each server
    [string]$DeployLogsDir = $GlobalConfiguration.DeployLogsDir                       # PowerShell Transcripts from each deploy live here
    [string]$ToolsPath     = $GlobalConfiguration.ToolsPath                           # Location of tools used by DBDeploy & MSBuild
    [string]$EntLibLoggingDirectory = $GlobalConfiguration.EntLibLoggingDirectory              # Enterprise Library MSMQ Logging Distributor
    [string]$StagingDirectory = $GlobalConfiguration.StagingDirectory
	[string]$ConfigurationScriptsDirectory = $GlobalConfiguration.ConfigurationScriptsDirectory
	[string]$GlobalConfigurationDirectoryPath = $GlobalConfiguration.GlobalConfigurationDirectoryPath
	[string]$SoftwareDirectory = $GlobalConfiguration.SoftwareDirectory
	[string]$DeplomentsScriptsDirectory = $GlobalConfiguration.DeplomentsScriptsDirectory
	[string]$DeploymentLogsDirectory = $GlobalConfiguration.DeploymentLogsDirectory
	[string]$EmailTmpDirectory = $GlobalConfiguration.EmailTmpDirectory
	[string]$EntLibLoggingInstallDirectory = $GlobalConfiguration.EntLibLoggingInstallDirectory #Enterprise Library Logging Install location
	[string]$UtilsDirectory = $GlobalConfiguration.UtilsDirectory

    # IIS Sites: Portal and Services tiers
    [string]$DotNetRunTimeVersion = $GlobalConfiguration.DotNetRunTimeVersion
    [string]$IISLogDirectory = $GlobalConfiguration.IISLogDirectory
    [string]$IISWebSiteRootDirectory = $GlobalConfiguration.IISWebSiteRootDirectory


	$ConfigFiles   = $GlobalConfiguration.ConfigFiles.ConfigFile                         # Environment specific configuration files
	
	$EnvironmentConfigurationFiles = $xmlInput.Configuration.Environments.Environment
	
	$CertificatesDirectory = $GlobalConfiguration.CertificatesDirectory
	
	# Enterprise Library Logging
	$QueueName = $GlobalConfiguration.QueueName
	$Transactional = $GlobalConfiguration.Transactional
	$Permission = $GlobalConfiguration.Permission
	
	# Azure Blob Storage - AzCopy
	$AzureBlobStorageUrl = $GlobalConfiguration.AzureBlobStorageUrl
	$AzureBlobStorageSourceKey = $GlobalConfiguration.AzureBlobStorageSourceKey
	$AzureBlobStorageDestKey = $GlobalConfiguration.AzureBlobStorageDestKey



	$content = @{
        rootSrcPath   = $rootSrcPath   	                        # The root directory where install binaries live
	    rootDestPath  = $rootDestPath                           # Installed binaries are placed here on each server
        DeployLogsDir = $DeployLogsDir                          # PowerShell Transcripts from each deploy live here
        ToolsPath     = $ToolsPath                              # Location of tools used by DBDeploy & MSBuild
        EntLibLoggingDirectory = $EntLibLoggingDirectory
	    StagingDirectory= $StagingDirectory

		ConfigurationScriptsDirectory = $ConfigurationScriptsDirectory
        GlobalConfigurationDirectoryPath = $GlobalConfigurationDirectoryPath
		SoftwareDirectory = $SoftwareDirectory
		DeplomentsScriptsDirectory = $DeplomentsScriptsDirectory
		DeploymentLogsDirectory = $DeploymentLogsDirectory
		EmailTmpDirectory = $EmailTmpDirectory
		EntLibLoggingInstallDirectory	= $EntLibLoggingInstallDirectory
	    UtilsDirectory = $UtilsDirectory

        # IIS Sites: Portal and Services tiers
        DotNetRunTimeVersion = $DotNetRunTimeVersion
        IISLogDirectory = $IISLogDirectory
        IISWebSiteRootDirectory = $IISWebSiteRootDirectory


	ConfigFiles   = $ConfigFiles                            # Environment specific configuration files

	EnvironmentConfigurationFiles = $EnvironmentConfigurationFiles

	CertificatesDirectory = $CertificatesDirectory

	    # Enterprise Library Logging
	    QueueName = $QueueName
	    Transactional = $Transactional
	    Permission = $Permission
		
		#Azure Blob Storage - AzCopy
		AzureBlobStorageUrl = $AzureBlobStorageUrl
		AzureBlobStorageSourceKey = $AzureBlobStorageSourceKey
		AzureBlobStorageDestKey = $AzureBlobStorageDestKey


        } # End of $content

    return [PSCustomObject]$content
 }

 # Returns a PSCustomObject that contains properties with all of the environment specific configuration values
 function Get-EnvironmentConfiguration([Parameter(Mandatory=$true)][string]$EnvironmentConfigurationFileName, [Parameter(Mandatory=$true)][string]$EnvironmentName, [Parameter(Mandatory=$false)][String]$XMLDirectory)
 {
    Write-Debug "In Get-EnvironmentConfiguration"
    # Append the path to the config file name and make sure it exists in the XML directory
    if (![string]::IsNullOrEmpty($EnvironmentConfigurationFileName))
    {
        $path = Get-XMLDirectory -XMLDirectory $XMLDirectory
        Write-Debug ("Get-EnvironmentConfiguration() - Path:  " + $path)
		$InputFile = (Join-Path -Path $path -ChildPath $EnvironmentConfigurationFileName)
    }

    if (!(Test-Path $InputFile))
    {
        Write-Host "Invalid Environment Configuration File Name specified:  $EnvironmentConfigurationFileName"
        Exit -1;
    }
    

    #Get the environment configuration from the file
    $xmlInput = [xml] (get-content $InputFile)
    $EnvironmentConfigurations = $xmlInput.Configuration.Environments.Environment

    # Loop through the environment configurations in the xml file and find
    # the environment configuration for the specified environment.
    foreach ($EnvironmentConfiguration in $EnvironmentConfigurations)
    {
        if ($EnvironmentConfiguration.Name -eq $EnvironmentName)
        {
            $Environment = $EnvironmentConfiguration
        }
    } 

    if ($Environment -eq $null)
    {
        Write-Host "Environment Name $EnvironmentName not found in $InputFile"
        exit 4
    }
        #Read each config value from the xml file into a local variable.
        $Name = $Environment.Name
		$DomainName = $Environment.DomainName
        $EnvironmentConfigFolderName = $Environment.EnvironmentConfigFolderName

        #Servers that installation binaries are copied to & Servers used in Remote PowerShell Sessions
        $PortalServers = @($Environment.PortalServers.Split())
        $MiddleTierServers = @($Environment.MiddleTierServers.Split())
        $SharePointServer = @($Environment.SharePointServer.Split())
	    $SharePointAPPs = @($Environment.SharePointAPPs.Split())
	    $SharePointWFEs = @($Environment.SharePointWFEs.Split())
	    $SqlServers = @($Environment.SqlServers.SqlServer)
        $ActiveDirectoryServers = @($Environment.ActiveDirectoryServers.Split())
        	
        # Web Sites
	    $AzureServicesWebSites = $Environment.AzureServicesWebSites.WebSite
	    $PortalWebSites	= $Environment.PortalWebSites.WebSite
	    $OnPremServicesWebSites	= $Environment.OnPremServicesWebSites.WebSite


	    #Unifed portal and services tier deployment flag
        [bool] $UseUnifiedPortalMiddleTier = [System.Convert]::ToBoolean($Environment.UseUnifiedPortalMiddleTier)

	    $ProductKeys = @($Environment.ProductKeys.ProductKey)
		
	    #Domain User name & password used for remote PowerShell Sessions on each server
        $RemotePowerShellUser = ($env:userdomain + "\" + $Environment.RemotePowerShellUser) # User name used for remote PowerShell Sessions on each server
        $RemotePowerShellPwd = $Environment.RemotePowerShellPwd                               # Password for remote PowerShell Sessions on each server

        # IIS Sites: Portal and Services tiers
        $IisSiteUserName = $Environment.IisSiteUserName
        $IisSiteUserPwd = $Environment.IisSiteUserPwd

        #Azure:  User name used to install Windows NT Services
	$WindowsServiceAzureUserName = $Environment.AzureServices.UserName
	$WindowsServiceAzureUserPwd = $Environment.AzureServices.Password
         
        $WindowsServicesAzure = $Environment.AzureServices.Service | Select-Object Name, DisplayName, StartupType, Description, RelativeExecutablePath
        
	#On-Premises Services
        [bool] $StartWindowsServicesOnPrem = [System.Convert]::ToBoolean($Environment.OnPremiseServices.StartWindowsServicesOnPrem)
        
	    $WindowsServicesOnPrem = $Environment.OnPremiseServices.Service | Select-Object Name, DisplayName, StartupType, Description, RelativeExecutablePath

		#OnPrem:  User name used to install Windows NT Services
		$WindowsServiceOnPremUserName = $Environment.OnPremiseServices.UserName
		$WindowsServiceOnPremUserPwd = $Environment.OnPremiseServices.Password

		# On-Premises
        	$OnPremServer = $Environment.OnPremServer

        # Search
        $CrawlAfterDeploy = $Environment.CrawlAfterDeploy

		#DBDeploy - SQL Server
		$dataSource = $Environment.dataSource
		$dbUser = $Environment.dbUser
		$dbPassword = $Environment.dbPassword
		
		# SQL Server
        $SQLServerNameSharePoint = $Environment.SQLServerNameSharePoint
		$SQLServerInstanceNameSharePoint = $Environment.SQLServerInstanceNameSharePoint
		$SQLInstances = $Environment.SQLServers.SQLServer.SQLInstances.SQLInstance
		$SQLAliases = @($Environment.SQLAliases.SQLAlias)
		
		# Certificates
		$Certificates   = $Environment.Certificates                         # Can contain multiple certificates
		
		# Active Directory
		$Accounts = @($Environment.Accounts.Account)
		
		# SharePoint
		$PassPhrase = $Environment.PassPhrase
		
        $SharePointFarmAdmins = @($Environment.SharePointFarmAdmins.SharePointFarmAdmin)

        #AppFabric
        #$SQLAlias = $Environment.AppFabric.SQLAlias
        #$PartnerSQLAlias = $Environment.AppFabric.PartnerSQLAlias
        #$SoftwareDirectory = $Environment.AppFabric.SoftwareDirectory
        #$AppFabricCacheDB = $Environment.AppFabric.AppFabricCacheDB
        #$StartingPort = $Environment.AppFabric.StartingPort
        #$AppPoolAccountName = $Environment.AppFabric.AppPoolAccountName
        #$cacheName = $Environment.AppFabric.cacheName

        # Create a custom PowerShell object (PSCustomObject) that contains all of the local variables
        # that contain the configuration values that were retrieved from the xml file.
        $content = @{
            Name = $Name
			DomainName = $DomainName
            EnvironmentConfigFolderName = $EnvironmentConfigFolderName
            #Servers that installation binaries are copied to & Servers used in Remote PowerShell Sessions
            PortalServers = @($PortalServers)
            MiddleTierServers = @($MiddleTierServers)
            SharePointServer = @($SharePointServer)
	    SharePointAPPs = @($SharePointAPPs)
	    SharePointWFEs = @($SharePointWFEs)
	    SqlServers = @($SqlServers)
            ActiveDirectoryServers = @($ActiveDirectoryServers)
            OnPremServer = $OnPremServer
	    UseUnifiedPortalMiddleTier = $UseUnifiedPortalMiddleTier
	    ProductKeys = @($ProductKeys)
		    #Domain User name & password used for remote PowerShell Sessions on each server
            RemotePowerShellUser = $RemotePowerShellUser
            RemotePowerShellPwd = $RemotePowerShellPwd

            # IIS Sites: Portal and Services tiers
            IisSiteUserName = $IisSiteUserName
            IisSiteUserPwd = $IisSiteUserPwd

            #Azure NT Services
		    WindowsServiceAzureUserName = $WindowsServiceAzureUserName
		    WindowsServiceAzureUserPwd = $WindowsServiceAzureUserPwd
            WindowsServicesAzure = $WindowsServicesAzure

		    #On-Premises NT Services Services
		    StartWindowsServicesOnPrem = $StartWindowsServicesOnPrem
		    WindowsServiceOnPremUserName = $WindowsServiceOnPremUserName
		    WindowsServiceOnPremUserPwd = $WindowsServiceOnPremUserPwd
            WindowsServicesOnPrem = $WindowsServicesOnPrem

            #Search 
            CrawlAfterDeploy = $CrawlAfterDeploy
            #DBDeploy - SQL Server
			dataSource = $dataSource
			dbUser = $dbUser
			dbPassword = $dbPassword
			
			# SQL Server
			SQLInstances = $SQLInstances
            SQLServerNameSharePoint = $SQLServerNameSharePoint
			SQLServerInstanceNameSharePoint = $SQLServerInstanceNameSharePoint	
			SQLAliases = $SQLAliases
			
			#Certificates
			Certificates = $Certificates
			
			#Active Directory
			Accounts = $Accounts
			
			# SharePoint
			PassPhrase = $PassPhrase
			SharePointFarmAdmins = $SharePointFarmAdmins

            # Web Sites
	        AzureServicesWebSites = $AzureServicesWebSites
	        PortalWebSites = $PortalWebSites
	        OnPremServicesWebSites = $OnPremServicesWebSites

            #AppFabric
            #SQLAlias = $SQLAlias
            #PartnerSQLAlias = $PartnerSQLAlias
           #SoftwareDirectory = $SoftwareDirectory
           #AppFabricCacheDB = $AppFabricCacheDB
           #StartingPort = $StartingPort
           #AppPoolAccountName = $AppPoolAccountName
           #cacheName = $cacheName
        }
       
        return [PSCustomObject]$content
 }
 
 function GetProductKey($Name, $ProductKeys)
 {
	foreach ($ProductKey in $ProductKeys)
	{
		if ($Name -eq $ProductKey.Name)
		{
			return $ProductKey.Key
		}
	}
		return $null
 }

 function GetAccountByToken ([string]$token, $Accounts)
{

	foreach ($Account in $Accounts)
	{
		

		if (![string]::IsNullOrEmpty($Account.GetElementsByTagName("Token").ToString()))
		{
		   
			if ($Account.token -eq $token)
			{
				#Write-Host $($Account.UserName)
				return $Account
			}
		}
	}
}
#endregion Configuration_Data_Functions

#region DotNet_Framework_Version
#Returns an array of versions of the .Net Framework installed.
#Supports versions 1.0 through 4.5c
function Get-DotNetFrameworkVersionsInstalled()
{
    $installedFrameworks = @()
    if(Test-Key2 "HKLM:\Software\Microsoft\.NETFramework\Policy\v1.0" "3705") { $installedFrameworks += "1.0" }
    if(Test-Key2 "HKLM:\Software\Microsoft\NET Framework Setup\NDP\v1.1.4322" "Install") { $installedFrameworks += "1.1" }
    if(Test-Key2 "HKLM:\Software\Microsoft\NET Framework Setup\NDP\v2.0.50727" "Install") { $installedFrameworks += "2.0" }
    if(Test-Key2 "HKLM:\Software\Microsoft\NET Framework Setup\NDP\v3.0\Setup" "InstallSuccess") { $installedFrameworks += "3.0" }
    if(Test-Key2 "HKLM:\Software\Microsoft\NET Framework Setup\NDP\v3.5" "Install") { $installedFrameworks += "3.5" }
    if(Test-Key2 "HKLM:\Software\Microsoft\NET Framework Setup\NDP\v4\Client" "Install") 
	{ 
		$installedFrameworks += "4.0c"
		If ((Get-ItemProperty "HKLM:\Software\Microsoft\NET Framework Setup\NDP\v4\Client").Version -like "4.5*") 
		{
			$installedFrameworks += "4.5c" 
		}
	}
	
    if(Test-Key2 "HKLM:\Software\Microsoft\NET Framework Setup\NDP\v4\Full" "Install") 
	{ 
		$installedFrameworks += "4.0"

		If ((Get-ItemProperty "HKLM:\Software\Microsoft\NET Framework Setup\NDP\v4\Full").Version -like "4.5*") 
		{
			$installedFrameworks += "4.5" 
		}
	}   
    return $installedFrameworks
}
 
function private:Test-Key([Parameter(Mandatory=$true)][String]$path, [Parameter(Mandatory=$true)][String]$key)
{
    if(!(Test-Path $path)) { return $false }
    if ((Get-ItemProperty $path).$key -eq $null) { return $false }
    return $true
}
#endregion DotNet_Framework_Version 

#region Certificates
# Import a certificate that requires a password
# usage: Import-PfxCertificate "C:\testCert.pfx" "LocalMachine" "My"
function Import-PfxCertificate{ 
param([String]$certPath,[String]$certRootStore = "CurrentUser",[String]$certStore = "My",$pfxPass = $null)
$pfx = new-object System.Security.Cryptography.X509Certificates.X509Certificate2

if ($pfxPass -eq $null) {$pfxPass = read-host "Enter the pfx password" -assecurestring}

$pfx.import($certPath,$pfxPass,"Exportable,PersistKeySet")

$store = new-object System.Security.Cryptography.X509Certificates.X509Store($certStore,$certRootStore)
$store.open("MaxAllowed")
$store.add($pfx)
$store.close()
}

# Import  X.509 certificates (.cer) that do not require a password.
# usage: Import-509Certificate "C:\HighTrustAriaCert.pfx" "LocalMachine" "My"
function Import-509Certificate {
param([String]$certPath,[String]$certRootStore,[String]$certStore)

$pfx = new-object System.Security.Cryptography.X509Certificates.X509Certificate2
$pfx.import($certPath)

$store = new-object System.Security.Cryptography.X509Certificates.X509Store($certStore,$certRootStore)   
$store.open("MaxAllowed")
$store.add($pfx)
$store.close()
}
#endregion Certificates
#region Zip
<#
.SYNOPSIS
    Creates a zip archive that contains the files and directories from the specified directory, and optionally includes the base directory.
.COMPONENTS
     .Net 4.5
.PARAMETER sourceDirectoryName
    The path to the directory to be archived, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.

.PARAMETER destinationArchiveFileName
    The path of the archive to be created, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.

.PARAMETER includeBaseDirectory
    true to include the directory name from sourceDirectoryName at the root of the archive; false to include only the contents of the directory.

.DESCRIPTION
    Uses the Optimal Compression setting.  The compression operation should be optimally compressed, even if the operation takes a longer time to complete.
#>
function Zip([Parameter(Mandatory=$true)][string]$SourceDirectoryName, [Parameter(Mandatory=$true)][string]$DestinationArchiveFileName, [Parameter(Mandatory=$true)][bool]$IncludeBaseDirectory)
{
    [System.Reflection.Assembly]::LoadWithPartialName('System.IO.Compression.FileSystem')

    # CompressionLevel:
    #    Fastest - The compression operation should complete as quickly as possible, even if the resulting file is not optimally compressed. 
    #    Compression - No compression should be performed on the file. 
    #    Optimal - The compression operation should be optimally compressed, even if the operation takes a longer time to complete.
    $CompressionLevel = [System.IO.Compression.CompressionLevel]::Optimal

    # Creates a zip archive that contains the files and directories from the specified directory, uses the specified compression level, and optionally includes the base directory.
    [System.IO.Compression.ZipFile]::CreateFromDirectory($sourceDirectoryName, $destinationArchiveFileName, $CompressionLevel, $includeBaseDirectory)

}

<#
.SYNOPSIS
    Extracts all the files in the specified zip archive to a directory on the file system and uses the specified character encoding for entry names.

.COMPONENTS
     .Net 4.5
.PARAMETER destinationDirectoryName
    The path to the directory in which to place the extracted files, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.

.PARAMETER sourceArchiveFileName
    The path to the archive that is to be extracted.

.DESCRIPTION
    Uses the Optimal Compression setting.  The compression operation should be optimally compressed, even if the operation takes a longer time to complete.
#>
function Unzip([Parameter(Mandatory=$true)][string]$SourceArchiveFileName, [Parameter(Mandatory=$true)][string]$DestinationDirectoryName)
{
 if(!(Test-Path $destinationDirectoryName))
   {
    [System.Reflection.Assembly]::LoadWithPartialName('System.IO.Compression.FileSystem')
    [System.IO.Compression.ZipFile]::ExtractToDirectory($sourceArchiveFileName, $destinationDirectoryName)
  }
}
#endregion Zip

#region Misc
#The way you determine the directory where the currently running script is located varies by version of PowerShell.
function Get-ScriptDirectory
{
        # May need to refactor to support different calling script locations

        Write-Debug "In Get-ScriptDirectory"
        $path = $null

        #Write-Debug ("PowerShell Major Version: " + $PSVersionTable.PSVersion.Major)
        if ($PSVersionTable.PSVersion.Major -eq 2)
        {
            $scriptInvocation = (Get-Variable MyInvocation -Scope 1).Value
            Write-Debug ("Script Name:  " + $scriptInvocation.ScriptName)
            $path = (Split-Path -Path (Split-Path $scriptInvocation.ScriptName -Parent) -Parent)
            Write-Debug "Get-ScriptDirectory() - Path:  $path"
            return $path
        }
        else
        {       
            #$path = (Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent)

            if ($PSScriptRoot -eq $null)
            {
                $path = Split-Path $psISE.CurrentFile.FullPath -Parent -Parent
            }
            else
            {
                $path = Split-Path $PSScriptRoot -Parent
            }

        }

        return $path
}

function Get-TranscriptFileName([Parameter(Mandatory=$false)][String]$BuildNumber, [Parameter(Mandatory=$true)][String]$DeployLogsDir)
{
	#Add the build number and date to the transcript file name.
	# Get the date
	$DateStamp = get-date -uformat "%Y-%m-%d@%H-%M-%S"
	$TranscriptFileName = $DeployLogsDir + "\Deploy_" + ($BuildNumber.Replace(".","-")) + "_" + $dateStamp +".txt"
	return $TranscriptFileName
}

function Test-Key2([string]$path, [string]$key)
{
    if(!(Test-Path $path)) { return $false }
    if ((Get-ItemProperty $path).$key -eq $null) { return $false }
    return $true
}

function MapNetworkDrive([Parameter(Mandatory=$true)][string]$DriveLetter, [Parameter(Mandatory=$true)][string]$ServerName, [Parameter(Mandatory=$true)][string]$ShareName)
{
	$net = $(New-Object -ComObject WScript.Network)
	$net.MapNetworkDrive($DriveLetter, "\\$ServerName\ShareName")
}

<#
.SYNOPSIS
    Shutdown server.

.PARAMETER Comment
    Comment associated with the reason of why the server is being shutdown.
    State what was installed or the script that requires the shutdown.

.PARAMETER Seconds
    The number of seconds to wait prior to shutdown.

.DESCRIPTION
    Shutdown server immediately as a planned maintenance.
#>
function ShutdownServer([Parameter(Mandatory=$true)][string]$Comment, [Parameter(Mandatory=$false)]$Seconds=0)
{
<#
/r         Shutdown and restart the computer.
    /t xxx     Set the time-out period before shutdown to xxx seconds.
               The valid range is 0-315360000 (10 years), with a default of 30.
               If the timeout period is greater than 0, the /f parameter is
               implied.
/c "comment" Comment on the reason for the restart or shutdown.
               Maximum of 512 characters allowed.
/f         Force running applications to close without forewarning users.
               The /f parameter is implied when a value greater than 0 is
               specified for the /t parameter.
    /d [p|u:]xx:yy  Provide the reason for the restart or shutdown.
               p indicates that the restart or shutdown is planned.
               u indicates that the reason is user defined.
               If neither p nor u is specified the restart or shutdown is
               unplanned.
Reasons on this computer:
(E = Expected U = Unexpected P = planned, C = customer defined)
Type	Major	Minor	Title
E   	4	1	Application: Maintenance (Unplanned)
E P 	4	1	Application: Maintenance (Planned)
E P 	4	2	Application: Installation (Planned)
#>


shutdown /r /t $seconds /c "$comment" /f /d p:4:1
}
#endregion Misc
#region SharePoint
#Adds a user or group as a SharePoint Farm admin and as PowerShell Admin
#Assumes the user or group already exists in Active Directory
function AddSharePointFarmAdmin([string]$UserNameOrGroup)
{

    Add-PSSnapin Microsoft.SharePoint.PowerShell -erroraction SilentlyContinue 

    # Creates a new Farm Administrator
    $caWebApp = Get-SPWebApplication -IncludeCentralAdministration | where-object {$_.DisplayName -eq "SharePoint Central Administration v4"} 
    $caSite = $caWebApp.Sites[0] 
    $caWeb = $caSite.RootWeb

    $farmAdministrators = $caWeb.SiteGroups["Farm Administrators"] 
    #AddUser - loginName, email, name, notes
    $farmAdministrators.AddUser($UserNameOrGroup, "", $UserNameOrGroup, "Configured via PowerShell")

    $caWeb.Dispose() 
    $caSite.Dispose()
    
    #Add the user as a Powershell Admin to the Central Admin Content DB and then to all of the other content databases in the farm
    $caDB = Get-SPContentDatabase -WebApplication $caWebApp 
    Add-SPShellAdmin -Database $caDB -Username $UserNameOrGroup

	Get-SPContentDatabase | Add-SPShellAdmin -Username $UserNameOrGroup

}
#endregion SharePoint
