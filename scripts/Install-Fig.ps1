# Script Settings
$FigBaseInstallLocation = "C:\Program Files\Fig"
$dbServer = "localhost"
$serviceName = "Fig.Api"
$logPath = "C:\ProgramData\fig"
$webPort = 7148
$apiPort = 7281
$figDbPassword = "495472f157684ab1a38fGGGPPP6f54e4aba64e"

# Downloads the latest release, deletes current installation and unzipts it to the install directory.
function Get-LatestFigRelease {
    $filenamePattern = "*.zip"

    $releasesUri = "https://api.github.com/repos/mzbrau/fig/releases/latest"
    $downloadUris = ((Invoke-RestMethod -Method GET -Uri $releasesUri).assets | Where-Object name -like $filenamePattern ).browser_download_url

    foreach ($uri in $downloadUris) {
        $pathZip = Join-Path -Path $([System.IO.Path]::GetTempPath()) -ChildPath $(Split-Path -Path $uri -Leaf)
        Write-Host "Downloading $uri"
        Invoke-WebRequest -Uri $uri -Out $pathZip
        
        if ($uri -match "Fig.Api") {
            $installLocation = Join-Path $FigBaseInstallLocation "Api"
        }
        elseif ($uri -match "Fig.Web") {
            $installLocation = Join-Path $FigBaseInstallLocation "Web"
        }
        elseif ($uri -match "Fig.Dpapi.Client") {
            $installLocation = Join-Path $FigBaseInstallLocation "DpapiClient"
        }
        else {
            Write-Host "Unknown file type $uri" -ForegroundColor Red
            continue
        }

        Write-Host "Removing existing installation"
        Remove-Item -Path $installLocation -Recurse -Force -ErrorAction SilentlyContinue

        Write-Host "Copying files to $installLocation"
        Expand-Archive -Path $pathZip -DestinationPath $installLocation -Force

        Remove-Item $pathZip -Force  
    }

    Write-Host "Files are downloaded and copied to $FigBaseInstallLocation" -ForegroundColor Green
}

# Creates website in iis
function Install-Website {
    Write-Host "Checking Website Installation"
    $existingWebsite = Get-IISSite 'Fig'

    if (-not ($existingWebsite)) {
        Write-Host "Installing Fig Web Application"
        $path = Join-Path $FigBaseInstallLocation "Web"
        New-IISSite -Name 'Fig' -PhysicalPath $path -BindingInformation "*:${webPort}:"
        Write-Host "IIS Website installed" -ForegroundColor Green
    }
}

# Creates a windows service for the API
Function Install-Service {    
    param(
        [Parameter(Mandatory = $true)][string]$NSSMPath,
        [Parameter(Mandatory = $true)][string]$serviceName,      
        [Parameter(Mandatory = $true)][string]$serviceExecutable,
        [Parameter(Mandatory = $true)][string]$serviceErrorLogFile,
        [Parameter(Mandatory = $true)][string]$serviceOutputLogFile,
        [Parameter(Mandatory = $true)][string]$arguments     
    )
    Write-Host Installing service $serviceName -ForegroundColor Green
    Write-Host "NSSM path"+$NSSMPath
    Write-Host $serviceName
    Write-Host $serviceExecutable
    Write-Host $serviceAppDirectory
    Write-Host $serviceErrorLogFile
    Write-Host $serviceOutputLogFile
    Write-Host $arguments

    if (-not (Test-Path $serviceExecutable)) {
        Write-Host "Cannot install service, does not exist at path $serviceExecutable"
        Exit
    }

    push-location
    Set-Location $NSSMPath

    $service = Get-Service $serviceName -ErrorAction SilentlyContinue

    if ($service) {
        Write-host service $service.Name is $service.Status
        Write-Host Removing $serviceName service   
        &.\nssm.exe stop $serviceName
        &.\nssm.exe remove $serviceName confirm
    }

    Write-Host Installing $serviceName as a service
    &.\nssm.exe install $serviceName $serviceExecutable $arguments
    &.\nssm.exe set $serviceName AppStderr $serviceErrorLogFile
    &.\nssm.exe set $serviceName AppStdout $serviceOutputLogFile

    Write-Host "Setting fig to run as local system"
    &.\nssm.exe set UT2003 ObjectName LocalSystem
    &.\nssm.exe set $serviceName AppStdoutCreationDisposition 2
    &.\nssm.exe set $serviceName AppStderrCreationDisposition 2
    &.\nssm.exe start $serviceName
    pop-location
}

function SetApiEnvironmentVariable {
    Write-Host "Setting Environment Variable for Fig URI" -ForegroundColor Green
    [Environment]::SetEnvironmentVariable('FIG_API_URI', "http://localhost:$apiPort", [EnvironmentVariableTarget]::Machine)
}

function Test-Administrator {  
    $user = [Security.Principal.WindowsIdentity]::GetCurrent();
    (New-Object Security.Principal.WindowsPrincipal $user).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)  
}

function Stop-ExistingService {
    $existingService = Get-Service $serviceName
    
    if ($null -ne $existingService) {
        Stop-Service $serviceName
    }
}

Function Get-IsFigServiceInstalled {
    Write-Host "Checking if Fig API is installed"
    $service = Get-Service $serviceName
    return $service -ne $null
}

function Get-UserInput {
    param(
        [Parameter( Mandatory = $true )]
        [String] $prompt
    )

    Write-Host "$($prompt): " -NoNewline -ForegroundColor Yellow
    return Read-Host
}

function Get-CheckDependencies {
    Write-Host "Checking Dependencies"
    $installedSoftware = Get-ChildItem "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall" | ForEach-Object { $_.GetValue('DisplayName') }
    $software = $installedSoftware -join ","

    if ($software -notmatch 'IIS URL Rewrite Module') {
        Write-Host "Install URL Rewrite module before continuing. https://www.iis.net/downloads/microsoft/url-rewrite" -ForegroundColor Yellow
        Exit
    }

	if (($software -notmatch '.NET AppHost Pack') -and ($software -notlike '*Microsoft .NET Host - 7*')) {
        Write-Host "Install app hosting bundle before continuing. https://dotnet.microsoft.com/en-us/download/dotnet/8.0" -ForegroundColor Yellow
        Exit
    }

    $WebSocketFeature = Get-WindowsFeature | Where-Object { $_.Name -eq "Web-WebSockets" }
    if ($WebSocketFeature -eq $null -and $WebSocketFeature.Installed -eq $false) {
        Write-Host "WebSocket Protocol feature is not installed or enabled in IIS." -ForegroundColor Yellow
    }

    Write-Host "Dependencies already installed." -ForegroundColor Green
}

function Install-RequiredModules {
    Write-Host "Checking required modules"
    if (Get-Module -ListAvailable -Name 'IISAdministration') {
        Write-Host "IISAdministration Module exists" -ForegroundColor Green
    } 
    else {
        Write-Host "Installing IISAdministration Module" -ForegroundColor Green
        Install-Module -Name 'IISAdministration'
    }
}

function Set-ApiSettings {	
    param(
        [String] $sentryDsn
    )
    
    Write-Host "Setting API Settings"
    $filePath = "$FigBaseInstallLocation\Api\appsettings.json"	
    $appSettingsJson = Get-Content -Raw $filePath | ConvertFrom-Json
    $newConnectionString = "Server=$dbServer;User Id=fig_login;Password=$figDbPassword;Initial Catalog=fig"
    $appSettingsJson.ApiSettings.DbConnectionString = $newConnectionString
    $computer = $env:computername
	$appSettingsJson.ApiSettings.WebClientAddresses = @( "http://$computer:$webPort" )

    if ($sentryDsn) {
        $appSettingsJson.ApiSettings.SentryDsn = $sentryDsn
    }
    
    $appSettingsJson | ConvertTo-Json -depth 16 | Set-Content $filePath
    Write-Host "Done" -ForegroundColor Green
}

function Set-WebSettings {
    param(
        [String] $sentryDsn
    )
    
    Write-Host "Setting Web Settings"
    $filePath = "$FigBaseInstallLocation\Web\wwwroot\appsettings.json"	
    $appSettingsJson = Get-Content -Raw $filePath | ConvertFrom-Json
    
    if ($sentryDsn) {
        $appSettingsJson.WebSettings.SentryDsn = $sentryDsn
    }

    $computer = $env:computername
	$appSettingsJson.WebSettings.ApiUri = "http://$computer:$apiPort"
    $appSettingsJson | ConvertTo-Json -depth 16 | Set-Content $filePath
    Write-Host "Done" -ForegroundColor Green
}

function Set-Settings {
    $webDsn = Get-UserInput "Enter the DSN for the web client. Leave blank if you don't have monitoring"
    $apiDsn = Get-UserInput "Enter the DSN for the api. Leave blank if you don't have monitoring"

    Set-WebSettings $webDsn
    Set-ApiSettings $apiDsn
}

function New-LogDir {
    if (-not (TestPath $logPath)) {
        New-Item -Path $logPath -ItemType Directory
    }
}

function Install-FigApi {
    if (-not (Get-IsFigServiceInstalled)) {
        Write-Host "Installing Fig API"
        $success = $false
		while (-not $success) {
            $nssmPath = Get-UserInput "Please enter the path to nssm.exe (without quotes). defaults to 'C:\Program Files\nssm-2.24\win64' if left blank. You can get NSSM from https://nssm.cc/download"

            if (-not($nssmPath)) {
                $nssmPath = 'C:\Program Files\nssm-2.24\win64'
            }
    
            $exePath = Join-Path $nssmPath "nssm.exe"
            if ((-not (Test-Path $nssmPath)) -and (-not (Test-Path $exePath))) {
                Write-Host "A valid path for NSSM is required"
            } else {
                $success = $true
            }
        }

        Install-Service $nssmPath $serviceName "$FigBaseInstallLocation\Api\Fig.Api.exe" "$logPath\fig.api.error.log" "$logPath\fig.api.log" "--urls http://0.0.0.0:$apiPort"
    }
    else {
        Stop-ExistingService
    }
}

function Setup-Database {

    Write-Host "In order to set up the database for Fig, the script needs to make a connection to your database. This connection string should be to the master database, and should be able to create a new database and user."
    $connectionString = Get-UserInput "Enter a connection string to the database where the Fig database should exist (leave blank for Server=localhost;Integrated Security=true)"
    
    if (-not($connectionString)) {
        $connectionString = "Server=localhost;Integrated Security=true"
    }

    $sqlFilePath = Join-Path $PSScriptRoot 'create-fig-database.sql'
    $sqlQuery = Get-Content -Path $sqlFilePath -Raw
    $sqlQuery = $sqlQuery -replace '<figDbPassword>', $figDbPassword
    $sqlConnection = New-Object System.Data.SqlClient.SqlConnection
    $sqlConnection.ConnectionString = $connectionString

    $sqlConnection.add_InfoMessage({
        param($sender, $eventArgs)
        Write-Host $eventArgs.Message
    })

    Write-Host "Query Was:"
    Write-Host $sqlQuery
    
    try {
        $sqlConnection.Open()
        $command = New-Object System.Data.SqlClient.SqlCommand 
        $command.CommandText = $sqlQuery
        $command.Connection = $sqlConnection
        $result = $command.ExecuteNonQuery()

        Write-Host "Fig database created / updated successfullly. Rows affected: $result"
    } catch {
        Write-Host "Error executing SQL query: $_" -ForegroundColor Red
    } finally {
        # Close the connection
        $sqlConnection.Close()
    }
}

function Get-ShouldAddClientSecret {
    while ($true) {
        $response = Read-Host "Do you want to add a client secret environment variable now? (Note that your client will need to run as the same user running this script) (Yes/No)"
        $response = $response.Trim().ToLower()

        if ($response -eq 'yes' -or $response -eq 'y') {
            return $true
        } elseif ($response -eq 'no' -or $response -eq 'n') {
            return $false
        } else {
            Write-Host "Invalid response. Please enter 'Yes' or 'No'."
        }
    }
}

function Set-ClientSecret {

    $addClient = Get-ShouldAddClientSecret

    # Check the user's response
    if ($addClient) {
        
        $rawClientName = Get-UserInput "Enter the name of your client. This should match the client name you are using in Fig"
        
        $clientName = $rawClientName.Replace(" ", "").ToUpper()

        $newGuid = [System.Guid]::NewGuid()
        $guidString = $newGuid.ToString()
        $encryptedGuid = [System.Security.Cryptography.ProtectedData]::Protect(
            [System.Text.Encoding]::UTF8.GetBytes($guidString),
            $null,
            [System.Security.Cryptography.DataProtectionScope]::CurrentUser)
        $envVarName = "FIG_${clientName}_SECRET"
        $encryptedGuidBase64 = [System.Convert]::ToBase64String($encryptedGuid)

        [Environment]::SetEnvironmentVariable($envVarName, $encryptedGuidBase64, [EnvironmentVariableTarget]::Machine)

        Write-Host "Set client secret environment variable called $envVarName. Note you'll need to restart visual studio for it to be read."
    }
}

function Get-IsInternetConnected {
    $uri = "https://github.com"
    $result = Invoke-WebRequest -Uri $uri -UseBasicParsing | Select-Object StatusCode

    if ($result.StatusCode -eq 200) {
        return $true
    }

    return $false
}

function Write-OfflineMessage {
    Write-Host "No Connection to the internet." -ForegroundColor Yellow
    Write-Host "The following manual steps are required:"
    Write-Host "1. Download the latest release from here: https://github.com/mzbrau/fig/releases"
    Write-Host "2. Extract the API to C:\Program Files\Fig\Api and the Web to C:\Program Files\Fig\Web and the dpapi client to C:\Program Files\Fig\DpapiClient"
    Write-Host "3. Ensure the powershell module 'IISAdministration' is installed"
    Write-Host "Push any key to continue..."
    Read-Host
}


## ************s
## Script Start
## ************
Write-Host "Fig Windows Installation"

$isAdmin = Test-Administrator
if (-not $isAdmin) {
    Write-Host "This script must be run as an administrator" -ForegroundColor Red
    Exit
}

Get-CheckDependencies
Install-RequiredModules
Setup-Database

if (Get-IsInternetConnected) {
    Get-LatestFigRelease
}
else {
    Write-OfflineMessage
}

Set-Settings
Install-FigApi
Install-Website
SetApiEnvironmentVariable
#Set-ClientSecret - not working at the moment.

Write-Host "Done" -ForegroundColor Green
Write-Host "Your fig API url is: http://localhost:$apiPort"
Write-Host "Your fig Web url is: http://localhost:$webPort"
Start-Process "http://localhost:$webPort"
