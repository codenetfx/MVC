<#
.SYNOPSIS
	Configure the Enterprise Library Distributor Service 

.PARAMETER user
	User name used to install the msmqdistributor.exe service.
	
.PARAMETER password 
		Password name used to install the msmqdistributor.exe service.
		
.PARAMETER permission 
	The only exceptable value is all.  If "all" is specified, then Full Control is granted to the user.  If not, 
	the following permissions are granted to the user instead:
	DeleteMessage, GenericWrite, PeekMessage, ReceiveJournalMessage

.PARAMETER transactional 

.PARAMETER QueueName

.PARAMETER uninstall
	Should we uninstall the Enterprise Library Logging MSMQ Distributor.
	
.PARAMETER EntLibLoggingInstallDirectory
	The directory where the Enterprise Logging is currently installed.

.DESCRIPTION
	Manage a clustered MSMQ instance for use with Enterprise Library Logging.  
	
	Install Performs the following:
	Installs MSMQ if its not installed.
	Creates a MSMQ Queue and sets permissions on the queue.  
	Installs the MSMQ Distributor
	
	Uninstall performs the following:
	The specified queue is deleted
	Unistalls the MSMQ Distributor
	
	To manage a clustered MSMQ instance, set the following environment variables in a PowerShell session:
	$env:computername = “myClusterResourceName”
	$env:_Cluster_Network_Hostname_ = “myClusterResourceName”
	$env:_Cluster_Network_Name_ = “myClusterResourceName”

.EXAMPLE
	Installation.
	.\SetupQueueDistributor.ps1 -user "DOMAIN\Web_Aria" -password "thepassword" -permission "all" -queuename ".\Private$\arialogging"

.EXAMPLE
	Uninstall.
	.\SetupQueueDistributor.ps1 -queuename ".\Private$\arialogging" -uninstall $true
#>
param(
[Parameter(Mandatory=$false)][string]$User, 
[Parameter(Mandatory=$false)][string]$Password, 
[Parameter(Mandatory=$false)][string]$Permission, 
[Parameter(Mandatory=$false)][bool]$Transactional, 
[Parameter(Mandatory=$false)][string]$QueueName, 
[Parameter(Mandatory=$false) ][bool]$Uninstall=$false, 
[Parameter(Mandatory=$false)][string]$EntLibLoggingInstallDirectory,
[Parameter(Mandatory=$false)]$EnvironmentName,
[Parameter(Mandatory=$false)]$ServerRole,
[Parameter(Mandatory=$false)][string]$GlobalConfigurationFileName,
[Parameter(Mandatory=$false)][String]$XMLDirectory
)

Set-StrictMode -Version 2

#Region External Functions
		. "$PSScriptRoot\Common\ConfigurationScriptFunctions.ps1"
#EndRegion External Functions

function DeleteQueue([Parameter(Mandatory=$true)][string]$QueueName)
{

 [Reflection.Assembly]::LoadWithPartialName("System.Messaging")
 [System.Messaging.MessageQueue]::Delete($QueueName)
 Write-Host "$QueueName uninstalled."
}



function InstallMSMQ()
{
	#Install MSMQ
	$restartRequired = $false;
    if ((Get-WindowsFeature -Name "MSMQ").Installed -eq $false){
	    Write-Host "MSMQ" -ForegroundColor Green
	    $feature = Add-WindowsFeature MSMQ
        $restartRequired = $restartRequired -or- ($feature.RestartNeeded -eq "Yes")
    }
	
    $restartRequired = $false;
    if ((Get-WindowsFeature -Name "MSMQ-Services").Installed -eq $false){
	    Write-Host "MSMQ-Services" -ForegroundColor Green
	    $feature = Add-WindowsFeature MSMQ-Services
        $restartRequired = $restartRequired -or- ($feature.RestartNeeded -eq "Yes")
    }
	
	return $restartRequired
}

function CreateQueue($QueueName, $Transactional, $Permission)
{
   [Reflection.Assembly]::LoadWithPartialName("System.Messaging")
   $Queue = New-Object System.Messaging.MessageQueue
 
    if ($Transactional -eq $true)
    {
    $Queue = [System.Messaging.MessageQueue]::Create($QueueName, $Transactional) 
    }
    else
    {
    $Queue = [System.Messaging.MessageQueue]::Create($QueueName) 
    }
    
	if($Queue -eq $null)
	{
		exit
	}

	$Queue.label = $QueueName
           
	return ([System.Messaging.MessageQueue]$Queue)
}

function SetQueuePermissions(
[Parameter(Mandatory=$true)][string]$QueueName, 
[Parameter(Mandatory=$true)][string]$User, 
[Parameter(Mandatory=$true)][string]$Permission)
{
[Reflection.Assembly]::LoadWithPartialName("System.Messaging")
$Queue = new-object System.Messaging.MessageQueue($QueueName)

	if ($Permission -eq "all")
	{
		Write-Host "Granting all permissions to "  $User
		([System.Messaging.MessageQueue]$Queue).SetPermissions($User, [System.Messaging.MessageQueueAccessRights]::FullControl, [System.Messaging.AccessControlEntryType]::Allow) 
	}
	else
	{
		Write-Host "Restricted Control for user: "  $User
		Write-Host ""
		([System.Messaging.MessageQueue]$Queue).SetPermissions($User, [System.Messaging.MessageQueueAccessRights]::DeleteMessage, [System.Messaging.AccessControlEntryType]::Set) 
		([System.Messaging.MessageQueue]$Queue).SetPermissions($User, [System.Messaging.MessageQueueAccessRights]::GenericWrite, [System.Messaging.AccessControlEntryType]::Allow) 
		([System.Messaging.MessageQueue]$Queue).SetPermissions($User, [System.Messaging.MessageQueueAccessRights]::PeekMessage, [System.Messaging.AccessControlEntryType]::Allow) 
		([System.Messaging.MessageQueue]$Queue).SetPermissions($User, [System.Messaging.MessageQueueAccessRights]::ReceiveJournalMessage, [System.Messaging.AccessControlEntryType]::Allow)
	}
}

function UpdateMsmqDistributorConfigurationQueueName(
[Parameter(Mandatory=$true)][string]$EntLibLoggingInstallDirectory, 
[Parameter(Mandatory=$true)][string]$QueueName
)
{
	 $configFile = $EntLibLoggingInstallDirectory + "\MsmqDistributor.exe.config"
     if(!(Test-Path $configFile))
     {
        throw "Can't find the MSMQ Distributor Configuration file:  $configFile"
     }
	 $doc = New-Object System.Xml.XmlDocument

	 $doc.Load($configFile)

	 $node = $doc.SelectSingleNode("/configuration/msmqDistributorSettings")
	 $node.Attributes["msmqPath"].Value = $queuename

	 $doc.Save($configFile)
}

function InstatllMsMqDistributorService(
[Parameter(Mandatory=$true)][string]$EntLibLoggingInstallDirectory,
[Parameter(Mandatory=$true)][string]$User,
[Parameter(Mandatory=$true)][string]$Password
)
{
	#Install Enterprise Library Distributor Service
	Invoke-Expression("$env:windir\Microsoft.NET\Framework64\v4.0.30319\installutil -i /username=$user /password=$password /unattended $EntLibLoggingInstallDirectory\msmqdistributor.exe")
	Set-Service "Enterprise Library Distributor Service" -StartupType Automatic
	Start-Service "Enterprise Library Distributor Service"
}

function UninstallMsMqDistriborService([Parameter(Mandatory=$true)][string]$EntLibLoggingInstallDirectory)
{
	Invoke-Expression("$env:windir\Microsoft.NET\Framework64\v4.0.30319\installutil /u $EntLibLoggingInstallDirectory\msmqdistributor.exe")
}
	
function Install(
[Parameter(Mandatory=$true)][string]$QueueName, 
[Parameter(Mandatory=$true)][bool]$Transactional, 
[Parameter(Mandatory=$true)][string]$User, 
[Parameter(Mandatory=$true)][string]$Password,
#[Parameter(Mandatory=$true)][string]$SoftwareDirectory,
[Parameter(Mandatory=$true)][string]$EntLibLoggingInstallDirectory
)
{
	[Reflection.Assembly]::LoadWithPartialName("System.Messaging")
	#Create MSMQ Queue
    #try{DeleteQueue($QueueName)}catch{}
    if ([System.Messaging.MessageQueue]::Exists($QueueName) -eq $false)
    {
	    $Queue = CreateQueue -QueueName $QueueName -Transactional $Transactional -Permission $Permission
		SetQueuePermissions -QueueName $QueueName -User $User -Permission $Permission
    }
	
	#InstallEnterpriseLibraryBinaries -SourceDirectory $SoftwareDirectory -DestinationDirectory $EntLibLoggingInstallDirectory
	UpdateMsmqDistributorConfigurationQueueName -EntLibLoggingInstallDirectory $EntLibLoggingInstallDirectory -QueueName $QueueName
	InstatllMsMqDistributorService -EntLibLoggingInstallDirectory $EntLibLoggingInstallDirectory  -User $User -Password $Password

}
		
function Uninstall([Parameter(Mandatory=$true)][string]$QueueName, [Parameter(Mandatory=$true)][string]$EntLibLoggingInstallDirectory)
{
	DeleteQueue($QueueName)
    UninstallMsMqDistriborService $EntLibLoggingInstallDirectory
    Remove-Item $EntLibLoggingInstallDirectory -Recurse
}

function InstallEnterpriseLibraryBinaries([Parameter(Mandatory=$true)][string]$SourceDirectory, [Parameter(Mandatory=$true)][string]$DestinationDirectory)
{
	if(!(Test-Path $DestinationDirectory)) {
		mkdir $DestinationDirectory | Out-Null
	}
	
	Copy-Item $SourceDirectory\*.* $DestinationDirectory -Recurse
}

function Main(
[Parameter(Mandatory=$false)][string]$User, 
[Parameter(Mandatory=$false)][string]$Password, 
[Parameter(Mandatory=$false)][string]$Permission, 
[Parameter(Mandatory=$false)][bool]$Transactional, 
[Parameter(Mandatory=$false) ][string]$QueueName, 
[Parameter(Mandatory=$true) ][bool]$Uninstall=$false, 
[Parameter(Mandatory=$false)][string]$EntLibLoggingInstallDirectory,
[Parameter(Mandatory=$false)]$EnvironmentName,
[Parameter(Mandatory=$false)]$ServerRole,
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
	
	#Get default values for input parameters
	if ([string]::IsNullOrEmpty($user)) 
		{
            $domain = Get-WindowsNtDomain
			$user = $domain + "\" + $EnvironmentConfiguration.WindowsServiceAzureUserName
			$Password = $EnvironmentConfiguration.WindowsServiceAzureUserPwd
		}
		
	if ([string]::IsNullOrEmpty($QueueName)) 
		{
			$QueueName = $GlobalConfiguration.QueueName
		}
		
	if ([string]::IsNullOrEmpty($Permission)) 
		{
			$Permission = $GlobalConfiguration.Permission
		}
		
		if ([string]::IsNullOrEmpty($Transactional)) 
		{
			$Transactional = $GlobalConfiguration.Transactional
		}
			
#	if ([string]::IsNullOrEmpty($SoftwareDirectory)) 
#		{
#			$SoftwareDirectory = $GlobalConfiguration.SoftwareDirectory
#		}
		
		
	if ([string]::IsNullOrEmpty($EntLibLoggingInstallDirectory))
		{
			$EntLibLoggingInstallDirectory = $GlobalConfiguration.EntLibLoggingInstallDirectory
		}
	
	#Begin Processing.
	if (!$uninstall)
	{
		#BUGBUG - If we install MSMQ & the script reboots server, we aren't going to install the MSMQ Distributor.
		$restartRequired = InstallMSMQ
		if($restartRequired) {
			Write-Host "A restart is required, the system will restart in 2 seconds..." -ForegroundColor Magenta
			Start-Sleep -s 2    
			Restart-Computer
			exit
			}
			
		Install -QueueName $QueueName -Transactional $Transactional -User $User -Password $Password -EntLibLoggingInstallDirectory $EntLibLoggingInstallDirectory
	}
	else
	{
		Uninstall -QueueName $QueueName -EnterpriseLibLoggingInstallDirectory $EntLibLoggingInstallDirectory
	}
}

Main -User $User -Password $Password -Permission $Permission -Transactional $Transactional -QueueName $QueueName -Uninstall $Uninstall -EntLibLoggingInstallDirectory $EntLibLoggingInstallDirectory -EnvironmentName $EnvironmentName -ServerRole $ServerRole -GlobalConfigurationFileName $GlobalConfigurationFileName -XMLDirectory $XMLDirectory
