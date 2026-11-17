$tokens = $null
$parseErrors = $null
$installer = [System.Management.Automation.Language.Parser]::ParseFile(
    (Join-Path $PSScriptRoot 'Install-Fig.ps1'), [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) {
    throw ($parseErrors | Out-String)
}

# Load only the functions, without running the machine-wide installation.
$functions = $installer.FindAll({
    param($node)
    $node -is [System.Management.Automation.Language.FunctionDefinitionAst]
}, $false) | ForEach-Object { $_.Extent.Text }
$functionScript = [System.Management.Automation.Language.Parser]::ParseInput(
    ($functions -join "`n"), (Join-Path $PSScriptRoot 'Install-Fig.ps1'),
    [ref]$tokens, [ref]$parseErrors)
. $functionScript.GetScriptBlock()

function Get-IISSite { param($Name) }
function New-IISSite { param($Name, $PhysicalPath, $BindingInformation) }

Describe 'Fig installer IIS setup' {
    BeforeEach {
        Mock Write-Host {}
        Mock Import-Module {}
        Mock Get-Command { [pscustomobject]@{ Name = 'IIS command' } } -ParameterFilter {
            $Name -contains 'Get-IISSite'
        }
    }

    It 'imports IISAdministration and verifies the site commands' {
        Install-RequiredModules

        Assert-MockCalled Import-Module -Times 1 -Exactly -Scope It -ParameterFilter {
            $Name -eq 'IISAdministration' -and $Global -and
            (($PSVersionTable.PSEdition -eq 'Core') -eq [bool]$UseWindowsPowerShell)
        }
        Assert-MockCalled Get-Command -Times 1 -Exactly -Scope It -ParameterFilter {
            $Name -contains 'Get-IISSite' -and $Name -contains 'New-IISSite'
        }
    }

    It 'stops with guidance when the module cannot be imported' {
        Mock Import-Module { throw 'import failed' }

        { Install-RequiredModules } | Should Throw 'import failed'
        Assert-MockCalled Get-Command -Times 0 -Exactly -Scope It -ParameterFilter {
            $Name -contains 'Get-IISSite'
        }
    }

    It 'stops when the imported module does not provide the site commands' {
        Mock Get-Command { throw 'command missing' } -ParameterFilter {
            $Name -contains 'Get-IISSite'
        }

        { Install-RequiredModules } | Should Throw 'command missing'
    }
}

Describe 'Fig installer website setup' {
    BeforeEach {
        $script:FigBaseInstallLocation = 'C:\Program Files\Fig'
        $script:webPort = 7148
        Mock Write-Host {}
        Mock Get-IISSite {}
        Mock New-IISSite {}
    }

    It 'creates the website when it does not exist' {
        Install-Website

        Assert-MockCalled New-IISSite -Times 1 -Exactly -Scope It -ParameterFilter {
            $Name -eq 'Fig' -and $PhysicalPath -eq 'C:\Program Files\Fig\Web' -and
            $BindingInformation -eq '*:7148:'
        }
    }

    It 'does not recreate an existing website' {
        Mock Get-IISSite { [pscustomobject]@{ Name = 'Fig' } }

        Install-Website

        Assert-MockCalled New-IISSite -Times 0 -Exactly -Scope It
    }

    It 'stops if querying IIS fails' {
        Mock Get-IISSite { throw 'IIS query failed' }

        { Install-Website } | Should Throw 'IIS query failed'
        Assert-MockCalled New-IISSite -Times 0 -Exactly -Scope It
    }

    It 'stops if creating the website fails' {
        Mock New-IISSite { throw 'IIS creation failed' }

        { Install-Website } | Should Throw 'IIS creation failed'
        Assert-MockCalled Write-Host -Times 0 -Exactly -Scope It -ParameterFilter {
            $Object -eq 'IIS Website installed'
        }
    }
}

Describe 'Fig installer database setup' {
    BeforeEach {
        $script:FigBaseInstallLocation = 'C:\Program Files\Fig'
        $script:figDbPassword = 'test-password'
        $script:loginExists = 0
        $script:executedBatches = New-Object 'System.Collections.Generic.List[string]'
        $script:failBatch = 0
        $script:connectionDisposed = $false
        $script:commandDisposed = $false
        $script:connection = [pscustomobject]@{ ConnectionString = '' }
        $script:connection | Add-Member ScriptMethod Open {}
        $script:connection | Add-Member ScriptMethod add_InfoMessage { param($handler) }
        $script:connection | Add-Member ScriptMethod Dispose { $script:connectionDisposed = $true }
        $script:command = [pscustomobject]@{ CommandText = ''; Connection = $null }
        $script:command | Add-Member ScriptMethod ExecuteNonQuery {
            $script:executedBatches.Add($this.CommandText)
            if ($script:executedBatches.Count -eq $script:failBatch) {
                throw 'SQL batch failed'
            }
            return -1
        }
        $script:command | Add-Member ScriptMethod Dispose { $script:commandDisposed = $true }
        $script:command | Add-Member ScriptMethod ExecuteScalar { return $script:loginExists }

        Mock Get-UserInput { '' }
        Mock Get-ExistingFigConnectionString { $null }
        Mock Get-RecoveryDatabasePassword { $null }
        Mock Save-RecoveryDatabasePassword {}
        Mock Read-FigDatabasePassword { 'existing-password' }
        Mock Test-FigDatabaseConnection {}
        Mock Write-Host {}
        Mock New-Object { $script:connection } -ParameterFilter {
            $TypeName -eq 'System.Data.SqlClient.SqlConnection'
        }
        Mock New-Object { $script:command } -ParameterFilter {
            $TypeName -eq 'System.Data.SqlClient.SqlCommand'
        }
    }

    It 'creates the database in a separate batch before selecting it' {
        Setup-Database

        $script:executedBatches.Count | Should Be 2
        $script:executedBatches[0] | Should Match 'CREATE DATABASE fig'
        $script:executedBatches[0] | Should Not Match 'USE fig'
        $script:executedBatches[1] | Should Match 'USE fig'
        $script:executedBatches[1] | Should Match "PASSWORD = 'test-password'"
        $script:executedBatches[1] | Should Match "SUSER_ID\('fig_login'\)"
        $script:executedBatches[1] | Should Match "sid = SUSER_SID\('fig_login'\)"
        ($script:executedBatches -join "`n") | Should Not Match '(?im)^\s*GO\s*$'
        $script:connectionDisposed | Should Be $true
        $script:commandDisposed | Should Be $true
    }

    It 'defaults to master with integrated security and a trusted server certificate' {
        Setup-Database

        $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $script:connection.ConnectionString
        $builder.DataSource | Should Be 'localhost'
        $builder.InitialCatalog | Should Be 'master'
        $builder.IntegratedSecurity | Should Be $true
        $builder.TrustServerCertificate | Should Be $true
    }

    It 'preserves an explicitly supplied connection string' {
        Mock Get-UserInput { 'Server=custom;Initial Catalog=master;Integrated Security=true;TrustServerCertificate=false' }

        Setup-Database

        $script:connection.ConnectionString | Should Be 'Server=custom;Initial Catalog=master;Integrated Security=true;TrustServerCertificate=false'
        (New-Object System.Data.SqlClient.SqlConnectionStringBuilder $script:figDbConnectionString).DataSource | Should Be 'custom'
    }

    It 'reuses and validates the existing API database connection' {
        $script:loginExists = 1
        $existing = 'Server=localhost;User Id=fig_login;Password=old-password;Initial Catalog=fig;TrustServerCertificate=True'
        Mock Get-ExistingFigConnectionString { $existing }

        Setup-Database

        $script:figDbConnectionString | Should Be $existing
        Assert-MockCalled Read-FigDatabasePassword -Times 0 -Exactly -Scope It
        Assert-MockCalled Test-FigDatabaseConnection -Times 1 -Exactly -Scope It -ParameterFilter {
            $ConnectionString -eq $existing
        }
    }

    It 'asks for the existing password when it cannot be recovered' {
        $script:loginExists = 1
        Mock Read-FigDatabasePassword { "existing'password" }

        Setup-Database

        Assert-MockCalled Read-FigDatabasePassword -Times 1 -Exactly -Scope It
        (New-Object System.Data.SqlClient.SqlConnectionStringBuilder $script:figDbConnectionString).Password | Should Be "existing'password"
        $script:executedBatches[1] | Should Not Match "existing''password"
    }

    It 'recovers the generated password after an interrupted installation' {
        $script:loginExists = 1
        Mock Get-RecoveryDatabasePassword { 'recovered-password' }

        Setup-Database

        Assert-MockCalled Read-FigDatabasePassword -Times 0 -Exactly -Scope It
        (New-Object System.Data.SqlClient.SqlConnectionStringBuilder $script:figDbConnectionString).Password | Should Be 'recovered-password'
    }

    It 'escapes a recovered password when creating a missing login' {
        Mock Get-RecoveryDatabasePassword { "new'password" }

        Setup-Database

        $script:executedBatches[1] | Should Match "PASSWORD = 'new''password'"
    }

    It 'stops when the existing settings point to a different SQL server' {
        Mock Get-ExistingFigConnectionString {
            'Server=other;User Id=fig_login;Password=old-password;Initial Catalog=fig'
        }

        { Setup-Database } | Should Throw 'different SQL Server'
        $script:executedBatches.Count | Should Be 0
    }

    It 'stops when the Fig login cannot connect' {
        Mock Test-FigDatabaseConnection { throw 'login validation failed' }

        { Setup-Database } | Should Throw 'login validation failed'
        $script:connectionDisposed | Should Be $true
        $script:commandDisposed | Should Be $true
    }

    It 'stops after a failed batch and disposes SQL resources' {
        $script:failBatch = 1

        { Setup-Database } | Should Throw 'SQL batch failed'

        $script:executedBatches.Count | Should Be 1
        $script:connectionDisposed | Should Be $true
        $script:commandDisposed | Should Be $true
        Assert-MockCalled Write-Host -Times 0 -Exactly -Scope It -ParameterFilter {
            $Object -like '*successfullly*'
        }
    }
}

Describe 'Fig installer file updates' {
    BeforeEach {
        $script:installPath = Join-Path $TestDrive 'Api'
        $script:releasePath = Join-Path $TestDrive 'release'
        $script:archivePath = Join-Path $TestDrive 'release.zip'
        New-Item -ItemType Directory -Path $script:installPath, $script:releasePath -Force | Out-Null
        Set-Content (Join-Path $script:installPath 'appsettings.json') 'existing settings'
        Set-Content (Join-Path $script:installPath 'appsettings.Production.json') 'custom settings'
        Set-Content (Join-Path $script:installPath 'web.config') 'custom IIS configuration'
        Set-Content (Join-Path $script:installPath 'old.dll') 'old'
        Set-Content (Join-Path $script:releasePath 'appsettings.json') 'default settings'
        Set-Content (Join-Path $script:releasePath 'new.dll') 'new'
        Compress-Archive -Path (Join-Path $script:releasePath '*') -DestinationPath $script:archivePath -Force
    }

    It 'updates release files while preserving local settings on repeated runs' {
        Update-FigFiles $script:archivePath $script:installPath
        Update-FigFiles $script:archivePath $script:installPath

        Get-Content (Join-Path $script:installPath 'appsettings.json') | Should Be 'existing settings'
        Get-Content (Join-Path $script:installPath 'appsettings.Production.json') | Should Be 'custom settings'
        Get-Content (Join-Path $script:installPath 'web.config') | Should Be 'custom IIS configuration'
        Test-Path (Join-Path $script:installPath 'new.dll') | Should Be $true
        Test-Path (Join-Path $script:installPath 'old.dll') | Should Be $false
        Test-Path "$script:installPath.fig-backup" | Should Be $false
        $backups = @(Get-ChildItem -LiteralPath $TestDrive -Directory -Filter 'Api.fig-backup-*')
        $backups.Count | Should Be 2
        @($backups | Where-Object { Test-Path (Join-Path $_.FullName 'old.dll') }).Count | Should Be 1
    }

    It 'installs files when the target directory does not yet exist' {
        $newPath = Join-Path $TestDrive 'new-root\NewApi'

        Update-FigFiles $script:archivePath $newPath

        Get-Content (Join-Path $newPath 'appsettings.json') | Should Be 'default settings'
        Test-Path (Join-Path $newPath 'new.dll') | Should Be $true
    }

    It 'restores an interrupted update before replacing files' {
        Move-Item $script:installPath "$script:installPath.fig-backup"

        Restore-InterruptedFileUpdate $script:installPath

        Get-Content (Join-Path $script:installPath 'appsettings.json') | Should Be 'existing settings'
        Test-Path "$script:installPath.fig-backup" | Should Be $false
    }

    It 'does not touch the installed files if extracting the release fails' {
        { Update-FigFiles (Join-Path $TestDrive 'missing.zip') $script:installPath } | Should Throw 'does not exist'

        Get-Content (Join-Path $script:installPath 'old.dll') | Should Be 'old'
        Get-Content (Join-Path $script:installPath 'appsettings.json') | Should Be 'existing settings'
    }

    It 'does not replace the installation with an incomplete archive' {
        { Update-FigFiles $script:archivePath $script:installPath @('Fig.Api.exe') } | Should Throw 'missing Fig.Api.exe'

        Get-Content (Join-Path $script:installPath 'old.dll') | Should Be 'old'
    }

    It 'restores the original files if the staged directory cannot be installed' {
        Mock Move-Item { throw 'move failed' } -ParameterFilter {
            $LiteralPath -like '*.fig-staging-*'
        }

        { Update-FigFiles $script:archivePath $script:installPath } | Should Throw 'move failed'

        Get-Content (Join-Path $script:installPath 'old.dll') | Should Be 'old'
        Get-Content (Join-Path $script:installPath 'appsettings.json') | Should Be 'existing settings'
        Test-Path "$script:installPath.fig-backup" | Should Be $false
    }
}

Describe 'Fig installer release discovery' {
    It 'rejects a release without both application archives before installing files' {
        Mock Invoke-RestMethod {
            [pscustomobject]@{ assets = @([pscustomobject]@{
                name = 'Fig.Api.zip'
                browser_download_url = 'https://example.invalid/Fig.Api.zip'
            }) }
        }

        { Get-LatestFigRelease } | Should Throw 'both Fig.Api and Fig.Web'
    }
}

Describe 'Fig installer password recovery' {
    It 'stores a generated password encrypted for the current Windows user' {
        $script:logPath = Join-Path $TestDrive 'recovery'
        Save-RecoveryDatabasePassword 'generated-password'

        $path = Join-Path $script:logPath 'installer-db-password.dpapi'
        Get-Content -Raw $path | Should Not Match 'generated-password'
        Get-RecoveryDatabasePassword | Should Be 'generated-password'
    }
}

Describe 'Fig installer service setup' {
    It 'removes a newly created service if its configuration fails' {
        $exe = Join-Path $TestDrive 'Fig.Api.exe'
        Set-Content $exe 'test'
        Mock Get-Service {}
        Mock Write-Host {}
        Mock Invoke-Nssm {}
        Mock Invoke-Nssm { throw 'configuration failed' } -ParameterFilter {
            $Arguments[0] -eq 'set' -and $Arguments[2] -eq 'AppStderr'
        }

        { Install-Service $TestDrive 'Fig.Api' $exe 'error.log' 'output.log' '--urls http://localhost:7281' } |
            Should Throw 'configuration failed'

        Assert-MockCalled Invoke-Nssm -Times 1 -Exactly -Scope It -ParameterFilter {
            $Arguments[0] -eq 'remove' -and $Arguments[1] -eq 'Fig.Api'
        }
    }
}

Describe 'Fig installer settings' {
    It 'preserves custom settings and origins when recovering an existing configuration' {
        $script:FigBaseInstallLocation = $TestDrive
        $apiPath = Join-Path $TestDrive 'Api'
        New-Item -ItemType Directory -Path $apiPath -Force | Out-Null
        $path = Join-Path $apiPath 'appsettings.json'
        $settings = @{ ApiSettings = @{ DbConnectionString = 'old'; WebClientAddresses = @('https://custom') }; Custom = 'kept' }
        $settings | ConvertTo-Json -Depth 10 | Set-Content $path
        $script:existingFigConnectionString = 'old'
        $script:figDbConnectionString = 'new'

        Set-ApiSettings

        $actual = Get-Content -Raw $path | ConvertFrom-Json
        $actual.ApiSettings.DbConnectionString | Should Be 'new'
        $actual.ApiSettings.WebClientAddresses[0] | Should Be 'https://custom'
        $actual.Custom | Should Be 'kept'
    }

    It 'adds local web origins on reruns without replacing existing origins or credentials' {
        $script:FigBaseInstallLocation = $TestDrive
        $script:webPort = 7148
        $apiPath = Join-Path $TestDrive 'Api'
        New-Item -ItemType Directory -Path $apiPath -Force | Out-Null
        $path = Join-Path $apiPath 'appsettings.json'
        @{ ApiSettings = @{ DbConnectionString = 'unchanged'; WebClientAddresses = @('https://custom') } } |
            ConvertTo-Json | Set-Content $path
        $script:existingFigConnectionString = 'unchanged'
        $script:figDbConnectionString = 'unchanged'

        Set-ApiSettings
        Set-ApiSettings

        $actual = Get-Content -Raw $path | ConvertFrom-Json
        $actual.ApiSettings.DbConnectionString | Should Be 'unchanged'
        ($actual.ApiSettings.WebClientAddresses -contains 'https://custom') | Should Be $true
        @($actual.ApiSettings.WebClientAddresses | Where-Object { $_ -eq 'http://localhost:7148' }).Count | Should Be 1
        @($actual.ApiSettings.WebClientAddresses | Where-Object { $_ -eq "http://$($env:COMPUTERNAME):7148" }).Count | Should Be 1
    }

    It 'configures local web origins for a fresh API installation' {
        $script:FigBaseInstallLocation = $TestDrive
        $script:webPort = 8148
        $apiPath = Join-Path $TestDrive 'Api'
        New-Item -ItemType Directory -Path $apiPath -Force | Out-Null
        $path = Join-Path $apiPath 'appsettings.json'
        @{ ApiSettings = @{ DbConnectionString = 'default' } } | ConvertTo-Json | Set-Content $path
        $script:existingFigConnectionString = $null
        $script:figDbConnectionString = 'new'

        Set-ApiSettings

        $actual = Get-Content -Raw $path | ConvertFrom-Json
        $actual.ApiSettings.DbConnectionString | Should Be 'new'
        ($actual.ApiSettings.WebClientAddresses -contains 'http://localhost:8148') | Should Be $true
        ($actual.ApiSettings.WebClientAddresses -contains "http://$($env:COMPUTERNAME):8148") | Should Be $true
    }

    It 'does not switch an existing SQLite configuration without confirmation' {
        $script:FigBaseInstallLocation = $TestDrive
        $apiPath = Join-Path $TestDrive 'Api'
        New-Item -ItemType Directory -Path $apiPath -Force | Out-Null
        $path = Join-Path $apiPath 'appsettings.json'
        @{ ApiSettings = @{ DbConnectionString = 'Data Source=fig.db;Version=3;New=True' } } |
            ConvertTo-Json | Set-Content $path
        Mock Get-UserInput { 'no' }

        { Get-ExistingFigConnectionString } | Should Throw 'without changing the existing SQLite configuration'

        (Get-Content -Raw $path | ConvertFrom-Json).ApiSettings.DbConnectionString | Should Match 'Version=3'
    }
}
