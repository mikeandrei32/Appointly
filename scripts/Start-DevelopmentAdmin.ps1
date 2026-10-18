$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$adminEmail = Read-Host 'Development administrator email (use fictional data)'
$securePassword = Read-Host 'Administrator password (12+ characters, uppercase, lowercase, number, symbol)' -AsSecureString
$keys = @('DevelopmentAdmin__Enabled', 'DevelopmentAdmin__Email', 'DevelopmentAdmin__Password')
$previous = @{}
foreach ($key in $keys) { $previous[$key] = [Environment]::GetEnvironmentVariable($key, 'Process') }
$passwordPointer = [IntPtr]::Zero
Push-Location $projectRoot
try {
    $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    $env:DevelopmentAdmin__Enabled = 'true'
    $env:DevelopmentAdmin__Email = $adminEmail
    $env:DevelopmentAdmin__Password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
    dotnet run --project backend/Appointly.Api --launch-profile http
    if ($LASTEXITCODE -ne 0) { throw 'API exited with an error. Check the startup output.' }
}
finally {
    foreach ($key in $keys) { [Environment]::SetEnvironmentVariable($key, $previous[$key], 'Process') }
    if ($passwordPointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer) }
    $securePassword.Dispose()
    Pop-Location
}
