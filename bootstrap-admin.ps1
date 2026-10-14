$ErrorActionPreference = 'Stop'

$securePassword = Read-Host 'Enter an administrator password (at least 12 characters)' -AsSecureString
$password = [System.Net.NetworkCredential]::new('', $securePassword).Password

try {
    if ($password.Length -lt 12) {
        throw 'The administrator password must contain at least 12 characters. The account was not changed.'
    }

    $env:MYSTUDENTS_BOOTSTRAP_ADMIN_PASSWORD = $password
    dotnet run --project (Join-Path $PSScriptRoot 'MyStudents.WebApi') --no-launch-profile -- --bootstrap-admin
    if ($LASTEXITCODE -ne 0) {
        throw "Administrator bootstrap failed (dotnet exit code $LASTEXITCODE)."
    }
}
finally {
    Remove-Item Env:MYSTUDENTS_BOOTSTRAP_ADMIN_PASSWORD -ErrorAction SilentlyContinue
    $password = $null
    if ($null -ne $securePassword) {
        $securePassword.Dispose()
    }
}
