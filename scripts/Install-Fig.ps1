# Script Settings
$FigBaseInstallLocation = "C:\Program Files\Fig"
$serviceName = "Fig.Api"
$logPath = "C:\ProgramData\fig"
$webPort = 7148
$apiPort = 7281
$figDbPassword = [System.Guid]::NewGuid().ToString("N")

# Downloads the latest release, deletes current installation and unzipts it to the install directory.
function Get-GitHubCredentials {
    Write-Host "GitHub API rate limit exceeded. Authentication required." -ForegroundColor Yellow
    $useAuth = Get-UserInput "Do you want to authenticate with GitHub? (yes/no)"
    
    if ($useAuth -eq "yes") {
        $username = Get-UserInput "Enter GitHub username"
        $password = Read-Host "Enter GitHub personal access token" -AsSecureString
        $cred = New-Object System.Management.Automation.PSCredential ($username, $password)
        return $cred
    }
    return $null
}

function Get-LatestFigRelease {
    $filenamePattern = "*.zip"
    $releasesUri = "https://api.github.com/repos/mzbrau/fig/releases/latest"
    
    try {
        # First try without authentication
        $response = Invoke-RestMethod -Method GET -Uri $releasesUri -ErrorAction Stop
        $downloadUris = @($response.assets | Where-Object name -like $filenamePattern | ForEach-Object { $_.browser_download_url })
    }
    catch {
        if ($_.Exception.Response.StatusCode.value__ -eq 403) {
            # Try with authentication
            $cred = Get-GitHubCredentials
            if ($null -ne $cred) {
                try {
                    $token = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($cred.Password))
                    $headers = @{
                        Authorization = "token $token"
                    }
                    $response = Invoke-RestMethod -Method GET -Uri $releasesUri -Headers $headers -ErrorAction Stop
                    $downloadUris = @($response.assets | Where-Object name -like $filenamePattern | ForEach-Object { $_.browser_download_url })
                }
                catch {
                    throw "Failed to authenticate with GitHub: $_"
                }
            }
            else {
                throw "GitHub authentication is required to download the release."
            }
        }
        else {
            throw "Failed to get latest release: $_"
        }
    }

    if (-not ($downloadUris -match 'Fig\.Api') -or -not ($downloadUris -match 'Fig\.Web')) {
        throw 'The latest release must contain both Fig.Api and Fig.Web archives. No files were updated.'
    }

    return $downloadUris
}

function Install-FigRelease {
    param([Parameter(Mandatory = $true)][string[]]$DownloadUris)

    foreach ($uri in $downloadUris) {
        if ($uri -match "Fig.Api") {
            $installLocation = Join-Path $FigBaseInstallLocation "Api"
            $requiredFiles = @('Fig.Api.exe', 'appsettings.json')
        }
        elseif ($uri -match "Fig.Web") {
            $installLocation = Join-Path $FigBaseInstallLocation "Web"
            $requiredFiles = @('wwwroot\index.html', 'wwwroot\appsettings.json')
        }
        elseif ($uri -match "Fig.Dpapi.Client") {
            $installLocation = Join-Path $FigBaseInstallLocation "DpapiClient"
            $requiredFiles = @('Fig.Dpapi.Client.exe')
        }
        else {
            Write-Host "Unknown file type $uri" -ForegroundColor Red
            continue
        }

        $pathZip = Join-Path ([System.IO.Path]::GetTempPath()) "fig-$([guid]::NewGuid().ToString('N')).zip"
        try {
            Write-Host "Downloading $uri"
            Invoke-WebRequest -Uri $uri -Out $pathZip -ErrorAction Stop
            Update-FigFiles $pathZip $installLocation $requiredFiles
        }
        finally {
            if (Test-Path $pathZip) {
                Remove-Item -LiteralPath $pathZip -Force -ErrorAction Stop
            }
        }
    }

    Write-Host "Files are downloaded and copied to $FigBaseInstallLocation" -ForegroundColor Green
}

function Restore-InterruptedFileUpdate {
    param([Parameter(Mandatory = $true)][string]$InstallLocation)

    $backupPath = "$InstallLocation.fig-backup"
    if (Test-Path $backupPath) {
        if (Test-Path $InstallLocation) {
            $archivePath = "$InstallLocation.fig-backup-$([guid]::NewGuid().ToString('N'))"
            Move-Item -LiteralPath $backupPath -Destination $archivePath -ErrorAction Stop
            Write-Host "Previous installation backed up to $archivePath"
        }
        else {
            Write-Host "Restoring interrupted update of $InstallLocation"
            Move-Item -LiteralPath $backupPath -Destination $InstallLocation -ErrorAction Stop
        }
    }
}

function Update-FigFiles {
    param(
        [Parameter(Mandatory = $true)][string]$ArchivePath,
        [Parameter(Mandatory = $true)][string]$InstallLocation,
        [string[]]$RequiredFiles = @()
    )

    Restore-InterruptedFileUpdate $InstallLocation
    $stagingPath = "$InstallLocation.fig-staging-$([guid]::NewGuid().ToString('N'))"
    $backupPath = "$InstallLocation.fig-backup"
    try {
        Expand-Archive -Path $ArchivePath -DestinationPath $stagingPath -ErrorAction Stop
        foreach ($requiredFile in $RequiredFiles) {
            if (-not (Test-Path -LiteralPath (Join-Path $stagingPath $requiredFile) -PathType Leaf)) {
                throw "The release archive is missing $requiredFile. The existing installation was not replaced."
            }
        }
        if (Test-Path $InstallLocation) {
            # Preserve local configuration while replacing the published binaries.
            Get-ChildItem -LiteralPath $InstallLocation -File -Recurse -Force -ErrorAction Stop | Where-Object {
                $_.Name -like 'appsettings*.json' -or $_.Name -eq 'web.config'
            } | ForEach-Object {
                $relativePath = $_.FullName.Substring($InstallLocation.TrimEnd('\').Length).TrimStart('\')
                $destination = Join-Path $stagingPath $relativePath
                $parent = Split-Path -Parent $destination
                if (-not (Test-Path $parent)) {
                    New-Item -ItemType Directory -Path $parent -Force -ErrorAction Stop | Out-Null
                }
                Copy-Item -LiteralPath $_.FullName -Destination $destination -Force -ErrorAction Stop
            }
            Move-Item -LiteralPath $InstallLocation -Destination $backupPath -ErrorAction Stop
        }

        Move-Item -LiteralPath $stagingPath -Destination $InstallLocation -ErrorAction Stop
        Restore-InterruptedFileUpdate $InstallLocation
        Write-Host "Files updated in $InstallLocation"
    }
    catch {
        if ((Test-Path $backupPath) -and -not (Test-Path $InstallLocation)) {
            Move-Item -LiteralPath $backupPath -Destination $InstallLocation -ErrorAction Stop
        }
        throw
    }
    finally {
        if (Test-Path $stagingPath) {
            Remove-Item -LiteralPath $stagingPath -Recurse -Force -ErrorAction Stop
        }
    }
}

# Creates website in iis
function Install-Website {
    Write-Host "Checking Website Installation"
    $existingWebsite = Get-IISSite 'Fig' -ErrorAction Stop

    if (-not ($existingWebsite)) {
        Write-Host "Installing Fig Web Application"
        $path = Join-Path $FigBaseInstallLocation "Web"
        New-IISSite -Name 'Fig' -PhysicalPath $path -BindingInformation "*:${webPort}:" -ErrorAction Stop
        Write-Host "IIS Website installed" -ForegroundColor Green
    }
}

# Creates a windows service for the API
function Invoke-Nssm {
    param([string]$Executable, [string[]]$Arguments)

    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "NSSM command '$($Arguments[0])' failed with exit code $LASTEXITCODE."
    }
}

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
    Write-Host $serviceErrorLogFile
    Write-Host $serviceOutputLogFile
    Write-Host $arguments

    if (-not (Test-Path $serviceExecutable)) {
        throw "Cannot install service; executable does not exist at $serviceExecutable"
    }

    $service = Get-Service $serviceName -ErrorAction SilentlyContinue
    if ($service) {
        throw "The $serviceName service already exists; it was not replaced."
    }

    $nssmExecutable = Join-Path $NSSMPath 'nssm.exe'
    Write-Host Installing $serviceName as a service
    Invoke-Nssm $nssmExecutable @('install', $serviceName, $serviceExecutable, $arguments)
    try {
        Invoke-Nssm $nssmExecutable @('set', $serviceName, 'AppStderr', $serviceErrorLogFile)
        Invoke-Nssm $nssmExecutable @('set', $serviceName, 'AppStdout', $serviceOutputLogFile)
        Invoke-Nssm $nssmExecutable @('set', $serviceName, 'ObjectName', 'LocalSystem')
        Invoke-Nssm $nssmExecutable @('set', $serviceName, 'AppStdoutCreationDisposition', '2')
        Invoke-Nssm $nssmExecutable @('set', $serviceName, 'AppStderrCreationDisposition', '2')
        Invoke-Nssm $nssmExecutable @('start', $serviceName)
    }
    catch {
        try {
            Invoke-Nssm $nssmExecutable @('remove', $serviceName, 'confirm')
        }
        catch {
            Write-Warning "Could not remove the partially configured $serviceName service: $_. Inspect it before rerunning."
        }
        throw
    }
}

function SetApiEnvironmentVariable {
    $uri = "http://localhost:$apiPort"
    $existingUri = [Environment]::GetEnvironmentVariable('FIG_API_URI', [EnvironmentVariableTarget]::Machine)
    if ([string]::IsNullOrWhiteSpace($existingUri)) {
        Write-Host "Setting Environment Variable for Fig URI" -ForegroundColor Green
        [Environment]::SetEnvironmentVariable('FIG_API_URI', $uri, [EnvironmentVariableTarget]::Machine)
    }
    elseif ($existingUri -ne $uri) {
        Write-Warning "Existing machine FIG_API_URI was preserved. Check that it points to the intended Fig API."
    }
}

function Test-Administrator {  
    $user = [Security.Principal.WindowsIdentity]::GetCurrent();
    (New-Object Security.Principal.WindowsPrincipal $user).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)  
}

function Stop-ExistingService {
    $existingService = Get-Service $serviceName -ErrorAction SilentlyContinue
    
    if ($null -ne $existingService -and $existingService.Status -ne 'Stopped') {
        Stop-Service $serviceName -ErrorAction Stop
    }
}

function Start-ExistingService {
    $existingService = Get-Service $serviceName -ErrorAction SilentlyContinue
    
    if ($null -ne $existingService -and $existingService.Status -ne 'Running') {
        Start-Service $serviceName -ErrorAction Stop
    }
}

Function Get-IsFigServiceInstalled {
    Write-Host "Checking if Fig API is installed"
    $service = Get-Service $serviceName -ErrorAction SilentlyContinue
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
        throw "Install URL Rewrite module before continuing: https://www.iis.net/downloads/microsoft/url-rewrite"
    }

    Write-Host "Dependencies already installed." -ForegroundColor Green
}

function Install-RequiredModules {
    Write-Host "Checking required modules"

    try {
        if ($PSVersionTable.PSEdition -eq 'Core') {
            Import-Module IISAdministration -UseWindowsPowerShell -Global -ErrorAction Stop
        }
        else {
            Import-Module IISAdministration -Global -ErrorAction Stop
        }

        Get-Command -Name Get-IISSite, New-IISSite -ErrorAction Stop | Out-Null
    }
    catch {
        throw "Unable to load the IISAdministration module and its IIS site commands. Enable IIS and IIS Management Scripts and Tools, then run this script as administrator. If using PowerShell 7, try Windows PowerShell 5.1. Details: $_"
    }

    Write-Host "IISAdministration module loaded" -ForegroundColor Green
}

function Write-FigSettings {
    param([Parameter(Mandatory = $true)]$Settings, [Parameter(Mandatory = $true)][string]$Path)

    $temporaryPath = "$Path.fig-staging-$([guid]::NewGuid().ToString('N'))"
    try {
        $Settings | ConvertTo-Json -Depth 16 -ErrorAction Stop | Set-Content -LiteralPath $temporaryPath -ErrorAction Stop
        [System.IO.File]::Replace($temporaryPath, $Path, [NullString]::Value)
    }
    finally {
        if (Test-Path -LiteralPath $temporaryPath) {
            Remove-Item -LiteralPath $temporaryPath -Force -ErrorAction Stop
        }
    }
}

function Set-ApiSettings {	
    
    Write-Host "Setting API Settings"
    $filePath = "$FigBaseInstallLocation\Api\appsettings.json"	
    $appSettingsJson = Get-Content -Raw $filePath -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
    $changed = $false
    if ($script:existingFigConnectionString -cne $script:figDbConnectionString) {
        $appSettingsJson.ApiSettings.DbConnectionString = $script:figDbConnectionString
        $changed = $true
    }

    $origins = @($appSettingsJson.ApiSettings.WebClientAddresses | Where-Object {
        -not [string]::IsNullOrWhiteSpace($_)
    })
    foreach ($origin in @("http://localhost:${webPort}", "http://$($env:COMPUTERNAME):${webPort}")) {
        if ($origins -notcontains $origin) {
            $origins += $origin
            $changed = $true
        }
    }
    if (-not $changed) {
        Write-Host "Existing API settings preserved"
        return
    }
    $appSettingsJson.ApiSettings | Add-Member -MemberType NoteProperty -Name WebClientAddresses -Value $origins -Force
    
    Write-FigSettings $appSettingsJson $filePath
    Write-Host "Done" -ForegroundColor Green
}

function Set-WebSettings {
    
    Write-Host "Setting Web Settings"
    if ($script:existingWebSettings) {
        Write-Host "Existing Web settings preserved"
        return
    }
    $filePath = "$FigBaseInstallLocation\Web\wwwroot\appsettings.json"	
    $appSettingsJson = Get-Content -Raw $filePath -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop

    $computer = $env:computername
	$appSettingsJson.WebSettings.ApiUri = "http://$($computer):$($apiPort)"
    Write-FigSettings $appSettingsJson $filePath
    Write-Host "Done" -ForegroundColor Green
}

function Set-Settings {
    Set-WebSettings
    Set-ApiSettings
}

function New-LogDir {
    if (-not (Test-Path $logPath)) {
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
            if (-not (Test-Path $exePath)) {
                Write-Host "A valid path for NSSM is required"
            } else {
                $success = $true
            }
        }

        New-LogDir
        Install-Service $nssmPath $serviceName "$FigBaseInstallLocation\Api\Fig.Api.exe" "$logPath\fig.api.error.log" "$logPath\fig.api.log" "--urls http://0.0.0.0:$apiPort"
    }
    else {
        Write-Host "Starting existing Fig API service after update"
        Start-ExistingService
    }
}

function Get-ExistingFigConnectionString {
    $filePath = Join-Path $FigBaseInstallLocation 'Api\appsettings.json'
    if (-not (Test-Path $filePath)) {
        return $null
    }

    $settings = Get-Content -Raw $filePath -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
    $connectionString = $settings.ApiSettings.DbConnectionString
    if ([string]::IsNullOrWhiteSpace($connectionString)) {
        throw "The existing API settings do not contain a database connection string: $filePath"
    }

    if ($connectionString -match '(?i)(?:^|;)\s*Version\s*=\s*3\s*(?:;|$)') {
        $answer = Get-UserInput "The existing API settings use SQLite. Switching to SQL Server does not migrate its data. Type yes to switch, or anything else to stop"
        if (([string]$answer).Trim() -ne 'yes') {
            throw 'Installation stopped without changing the existing SQLite configuration.'
        }
        return $null
    }

    try {
        $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $connectionString
    }
    catch {
        throw "The existing API database connection string is invalid. Correct it before continuing."
    }
    if ($builder.UserID -ne 'fig_login' -or $builder.IntegratedSecurity -or $builder.InitialCatalog -ne 'fig') {
        throw "The existing API database connection is not configured for fig_login and the fig database. Preserve it and update Fig manually."
    }
    return $connectionString
}

function Read-FigDatabasePassword {
    $securePassword = Read-Host 'Enter the existing fig_login password' -AsSecureString
    $password = (New-Object System.Net.NetworkCredential('', $securePassword)).Password
    if ([string]::IsNullOrEmpty($password)) {
        throw 'The existing fig_login password is required.'
    }
    return $password
}

function Get-RecoveryDatabasePassword {
    $path = Join-Path $logPath 'installer-db-password.dpapi'
    if (-not (Test-Path $path)) {
        return $null
    }

    Add-Type -AssemblyName System.Security
    try {
        $bytes = [Convert]::FromBase64String((Get-Content -LiteralPath $path -Raw -ErrorAction Stop).Trim())
        $plain = [System.Security.Cryptography.ProtectedData]::Unprotect(
            $bytes, $null, [System.Security.Cryptography.DataProtectionScope]::CurrentUser)
        return [Text.Encoding]::UTF8.GetString($plain)
    }
    catch {
        Write-Warning "Could not recover the installer password for the current Windows user. You will need the existing fig_login password. Details: $_"
        return $null
    }
}

function Save-RecoveryDatabasePassword {
    param([Parameter(Mandatory = $true)][string]$Password)

    New-LogDir
    Add-Type -AssemblyName System.Security
    $bytes = [System.Security.Cryptography.ProtectedData]::Protect(
        [Text.Encoding]::UTF8.GetBytes($Password), $null,
        [System.Security.Cryptography.DataProtectionScope]::CurrentUser)
    $path = Join-Path $logPath 'installer-db-password.dpapi'
    [Convert]::ToBase64String($bytes) | Set-Content -LiteralPath $path -ErrorAction Stop
}

function Test-FigDatabaseConnection {
    param([Parameter(Mandatory = $true)][string]$ConnectionString)
    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
    }
    finally {
        $connection.Dispose()
    }
}

function Setup-Database {

    Write-Host "In order to set up the database for Fig, the script needs to make a connection to your database. This connection string should be to the master database, and should be able to create a new database and user."
    $connectionString = Get-UserInput "Enter a connection string to the database where the Fig database should exist (leave blank for Server=localhost;Initial Catalog=master;Integrated Security=true;TrustServerCertificate=true)"
    
    if (-not($connectionString)) {
        $connectionString = "Server=localhost;Initial Catalog=master;Integrated Security=true;TrustServerCertificate=true"
    }

    $adminBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $connectionString
    $script:existingFigConnectionString = Get-ExistingFigConnectionString
    if ($script:existingFigConnectionString) {
        $figBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $script:existingFigConnectionString
        if ($figBuilder.DataSource -ne $adminBuilder.DataSource) {
            throw 'The existing API settings point to a different SQL Server than the supplied administrative connection. Use the same server to continue.'
        }
    }
    else {
        $figBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $connectionString
        $figBuilder['Integrated Security'] = $false
        $figBuilder['User ID'] = 'fig_login'
        $figBuilder['Initial Catalog'] = 'fig'
        $figBuilder['Password'] = ''
    }

    $sqlFilePath = Join-Path $PSScriptRoot 'create-fig-database.sql'
    $sqlQuery = Get-Content -Path $sqlFilePath -Raw
    $sqlConnection = New-Object System.Data.SqlClient.SqlConnection
    $sqlConnection.ConnectionString = $connectionString

    $sqlConnection.add_InfoMessage({
        param($sender, $eventArgs)
        Write-Host $eventArgs.Message
    })

    $command = $null
    try {
        $sqlConnection.Open()
        $command = New-Object System.Data.SqlClient.SqlCommand 
        $command.Connection = $sqlConnection
        $command.CommandText = "SELECT CASE WHEN SUSER_ID('fig_login') IS NULL THEN 0 ELSE 1 END"
        $loginExists = [int]$command.ExecuteScalar() -eq 1
        if ($figBuilder.Password) {
            $figDbPassword = $figBuilder.Password
        }
        else {
            $recoveryPassword = Get-RecoveryDatabasePassword
            if ($recoveryPassword) {
                $figDbPassword = $recoveryPassword
            }
            elseif ($loginExists) {
                $figDbPassword = Read-FigDatabasePassword
            }
            else {
                Save-RecoveryDatabasePassword $figDbPassword
            }
        }
        $figBuilder['Password'] = $figDbPassword
        # Do not send an existing login's password in the database setup batch.
        $createPassword = if ($loginExists) { [guid]::NewGuid().ToString('N') } else { $figDbPassword }
        $escapedPassword = $createPassword.Replace("'", "''")
        $sqlQuery = $sqlQuery.Replace('<figDbPassword>', $escapedPassword)

        $result = 0
        # GO is a client-side batch separator, not a SQL Server statement.
        foreach ($batch in [regex]::Split($sqlQuery, '(?im)^\s*GO\s*$')) {
            if (-not [string]::IsNullOrWhiteSpace($batch)) {
                $command.CommandText = $batch
                $result += $command.ExecuteNonQuery()
            }
        }

        if ($script:existingFigConnectionString -and
            (New-Object System.Data.SqlClient.SqlConnectionStringBuilder $script:existingFigConnectionString).Password) {
            $script:figDbConnectionString = $script:existingFigConnectionString
        }
        else {
            $script:figDbConnectionString = $figBuilder.ConnectionString
        }
        try {
            Test-FigDatabaseConnection $script:figDbConnectionString
        }
        catch {
            $exception = $_.Exception
            if ($exception -is [System.Management.Automation.MethodInvocationException]) {
                $exception = $exception.InnerException
            }
            if (-not $loginExists -or $exception -isnot [System.Data.SqlClient.SqlException] -or $exception.Number -ne 18456) {
                throw
            }
            Write-Warning 'The saved fig_login password was rejected. Enter the current password.'
            $figBuilder['Password'] = Read-FigDatabasePassword
            $script:figDbConnectionString = $figBuilder.ConnectionString
            Test-FigDatabaseConnection $script:figDbConnectionString
        }
        Write-Host "Fig database created / updated successfullly. Rows affected: $result"
    } catch {
        throw "Error executing SQL query: $_"
    } finally {
        if ($null -ne $command) {
            $command.Dispose()
        }
        $sqlConnection.Dispose()
    }
}

function Get-ShouldAddClientSecret {
    while ($true) {
        $response = Read-Host "Do you want to add a client secret environment variable now? (Yes/No)"
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
            [System.Security.Cryptography.DataProtectionScope]::LocalMachine)
        $envVarName = "FIG_${clientName}_SECRET"
        $encryptedGuidBase64 = [System.Convert]::ToBase64String($encryptedGuid)

        [Environment]::SetEnvironmentVariable($envVarName, $encryptedGuidBase64, [EnvironmentVariableTarget]::Machine)

        Write-Host "Set client secret environment variable called $envVarName. Note you'll need to restart visual studio for it to be read."
    }
}

function Get-IsInternetConnected {
    $uri = "https://github.com"
    $result = Invoke-WebRequest -Uri $uri -UseBasicParsing -ErrorAction Stop | Select-Object StatusCode

    if ($result.StatusCode -eq 200) {
        return $true
    }

    return $false
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
foreach ($directory in @('Api', 'Web', 'DpapiClient')) {
    Restore-InterruptedFileUpdate (Join-Path $FigBaseInstallLocation $directory)
}
$script:existingWebSettings = Test-Path (Join-Path $FigBaseInstallLocation 'Web\wwwroot\appsettings.json')
if (-not (Get-IsInternetConnected)) {
    throw 'An internet connection is required to update Fig from the latest GitHub release.'
}
$releaseUris = @(Get-LatestFigRelease)
Setup-Database

# Stop a running API before replacing binaries on upgrade
Stop-ExistingService

try {
    Install-FigRelease $releaseUris
}
catch {
    Write-Warning "The Fig API service may remain stopped after the failed update. Rerun the installer after resolving the error."
    throw
}

Set-Settings
$recoveryPasswordPath = Join-Path $logPath 'installer-db-password.dpapi'
if (Test-Path $recoveryPasswordPath) {
    Remove-Item -LiteralPath $recoveryPasswordPath -Force -ErrorAction Stop
}
Install-FigApi
Install-Website
SetApiEnvironmentVariable
#Set-ClientSecret - not working at the moment.

Write-Host "Done" -ForegroundColor Green
Write-Host "Your fig API url is: http://localhost:$apiPort"
Write-Host "Your fig Web url is: http://localhost:$webPort"
Start-Process "http://localhost:$webPort"
