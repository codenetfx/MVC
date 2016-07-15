<#
.SYNOPSIS
	Manage SQL Server Aliases

.DESCRIPTION
	Directly updates the Registry to manage SQL Server aliases.
	Doesn't use WMI.

.EXAMPLE
	get-sqlalias "sqlserver"
	
.EXAMPLE
	remove-sqlalias "sqlserver"
	
.EXAMPLE
	add-sqlalias "sqlserver" "sp2013"  
#>

Set-StrictMode -Version 2

#region Internal_Functions

# This function manipulates the Windows Registry
function SqlAlias([Parameter(Mandatory=$true)][string]$Name, [int]$Action, [Parameter(Mandatory=$false)][string][string]$Server, [Parameter(Mandatory=$true)][string]$Key, [string]$Value)
{

	# Constant value
	[string]$Hive  = "LocalMachine";
    
    if ([string]::IsNullOrEmpty($Server)) {$Server = $env:COMPUTERNAME}

    try
    {
        $reg = [Microsoft.Win32.RegistryKey]::OpenRemoteBaseKey([Microsoft.Win32.RegistryHive]$Hive, $Server);

        # Open subkey depending of action in read or write mode.
        if ($action -eq 0)
        {    $subKey = $reg.OpenSubKey($key, $false);   }
        else
        {    $subKey = $reg.OpenSubKey($key, $true);   }

        # If the ConnectTo key doesn't exist, create it.
        if(!$subKey -and ($action -eq 1)) #Action = Add
        {
            Write-Output "Creating Key on machine $Server";
            $reg.CreateSubKey($key);
            $subKey = $reg.OpenSubKey($key, $true);
        }

        if(!$subKey)
        {
            Write-Output "Key not found on machine $Server";
            Continue;
        }

        try
        {
            $res = $subKey.GetValue($Name);
            switch ($action)
            {
                0 # Read reg key and prompt result.
                {
                    if(!$res)
                    {   Write-Output "Value doesn't exists on $Server";   }
                    else
                    {   Write-Output "Value on machine $Server = $res";   }
                }

                1 # Add / edit alias.
                {
                    $subKey.SetValue($Name, $Value);
                    if(!$res)
                    {    Write-Output "Value added on machine $Server";   }
                    else
                    {   Write-Output "Value updated on machine $Server";   }
                }

                2 # Delete value.
                {
                    if(!$res)
                    {   Write-Output "Nothing to delete on machine $Server";   }
                    else
                    {
                        $subKey.DeleteValue($Name, $true);
                        Write-Output "Value successfully deleted on machine $Server";
                    }
                }

                default
                {   Write-Output "Unknown action.";   }
            }
        }
        catch
        {
            Write-Output ($_.Exception.Message);
        }
    }
    catch
    {
         Write-Output ("Error on " + $Server + ": " + $_.Exception.Message);
    }

    $reg.Close();
}

function Get-Key32()
{
	return "SOFTWARE\\Wow6432Node\\Microsoft\\MSSQLServer\\Client\\ConnectTo";
}

function Get-Key64()
{
	return "SOFTWARE\\Microsoft\\MSSQLServer\\Client\\ConnectTo";
}

function add-sqlalias32([Parameter(Mandatory=$true)][string]$Name, [Parameter(Mandatory=$false)][string]$Server, [Parameter(Mandatory=$true)][string]$SqlServerName, [Parameter(Mandatory=$false)][string]$SqlInstance, $Port=1433, $Protocol="tcp")
{
	$Action = 1 # 1 = add/update value
	[string]$Key = Get-Key32
	
    # Append the sql instance name if it was provided.
    if (!([String]::IsNullOrEmpty($SQLInstance)))
    {
       
        $SQLServName = $SqlServerName + "\" + $SQLInstance
    }

	# Configuration of Alias with protocol and target address.
	switch ($Protocol) 
    { 
        "tcp"{
				$Protocol = "DBMSSOCN"
				[string]$Value = "$Protocol,$SqlServerName,$Port";
			} 
        "np"{
				$Protocol = "np"
				[string]$Value = "\\$SqlServerName\pipe\sql\query";
			} 
        "via"{
				$Protocol = "via"
				[string]$Value = "$protocol";
			} 
        default {throw "Unknown protocol."}
    }

	SqlAlias -Name $Name  -Action $Action -Server $Server -Key $Key -Value $Value
}

function add-sqlalias64([Parameter(Mandatory=$true)][string]$name, [Parameter(Mandatory=$false)][string]$Server, [Parameter(Mandatory=$true)][string]$SqlServerName, [Parameter(Mandatory=$false)][string]$SqlInstance, [Parameter(Mandatory=$false)][string]$port=1433, [Parameter(Mandatory=$false)][string]$protocol="tcp")
{
	$Action = 1 # 1 = add/update value
	[string]$Key = Get-Key64
	
    # Append the sql instance name if it was provided.
    if (!([String]::IsNullOrEmpty($SQLInstance)))
    {
       
        $SQLServName = $SqlServerName + "\" + $SQLInstance
    }

	# Configuration of Alias with protocol and target address.
	switch ($Protocol) 
    { 
        "tcp"{
				$Protocol = "DBMSSOCN"
				[string]$Value = "$Protocol,$SqlServerName,$Port";
			} 
        "np"{
				$Protocol = "np"
				[string]$Value = "\\$SqlServerName\pipe\sql\query";
			} 
        "via"{
				$Protocol = "via"
				[string]$Value = "$protocol";
			} 
        default {throw "Unknown protocol."}
    }
	
	SqlAlias -Name $Name  -Action $Action -Server $Server -Key $Key -Value $Value
}

function remove-sqlalias32([Parameter(Mandatory=$true)][string]$Name, [Parameter(Mandatory=$false)][string]$Server)
{
	$Action = 2 # 2 = delete value.
	[string]$Key = Get-Key32
	
	SqlAlias -Name $Name  -Action $Action -Server $Server -Key $Key #-Value $Value
}

function remove-sqlalias64([Parameter(Mandatory=$true)][string]$Name, [Parameter(Mandatory=$false)][string]$Server)
{
	$Action = 2 # 2 = delete value.
	[string]$Key = Get-Key64

	SqlAlias -Name $Name  -Action $Action -Server $Server -Key $Key #-Value $Value
}

function get-sqlalias32 ([Parameter(Mandatory=$true)][string]$Name, [Parameter(Mandatory=$false)][string]$Server)
{
	$Action = 0 # read
	[string]$Key = Get-Key32
	
	SqlAlias -Name $Name  -Action $Action -Server $Server -Key $Key #-Value $Value
}

function get-sqlalias64 ([Parameter(Mandatory=$true)][string]$Name, [Parameter(Mandatory=$false)][string]$Server) 
{
	
	$Action = 0  # read
	[string]$Key = Get-Key64
	
	SqlAlias -Name $Name  -Action $Action -Server $Server -Key $Key #-Value $Value
}
#endregion Internal_Functions

function get-sqlalias ([Parameter(Mandatory=$true)][string]$Name, [Parameter(Mandatory=$false)][string]$Server)
{
    get-sqlalias32 -Name $Name -Server $Server
    get-sqlalias64 -Name $Name -Server $Server
}

function remove-sqlalias([Parameter(Mandatory=$true)][string]$Name, [Parameter(Mandatory=$false)][string]$Server)
{
    remove-sqlalias32 -Name $Name -Server $Server
    remove-sqlalias64 -Name $Name -Server $Server
}

function add-sqlalias([Parameter(Mandatory=$false)][string]$Server, [Parameter(Mandatory=$true)][string]$Name, [Parameter(Mandatory=$true)][string]$SqlServerName, [Parameter(Mandatory=$false)][string]$SqlInstance, [Parameter(Mandatory=$false)][string]$Port=1433, [Parameter(Mandatory=$false)][string]$Protocol="tcp")
{
    add-sqlalias32 -Name $Name -Server $server -SqlServerName $SqlServerName -SqlInstance $SqlInstance -Port $Port -Protocol $Protocol
    add-sqlalias64 -Name $Name -Server $server -SqlServerName $SqlServerName -SqlInstance $SqlInstance -Port $Port -Protocol $Protocol
}

#Internal Functions:  
# get-sqlalias64, get-sqlalias32, add-sqlalias64, add-sqlalias32, remove-sqlalias64, remove-sqlalias32

#Convert into a module:
# 1) uncomment the Export-Module line below
# 2) Rename the file with extension ".psm1". 
# 3) Use the file as PowerShell module with cmdlet "import-module"
#Export-ModuleMember -Function get-sqlalias,  add-sqlalias, remove-sqlalias


