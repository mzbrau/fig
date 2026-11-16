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

Describe 'Fig installer database setup' {
    BeforeEach {
        $script:figDbPassword = 'test-password'
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

        Mock Get-UserInput { '' }
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
