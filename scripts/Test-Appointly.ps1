$ErrorActionPreference = 'Stop'

function Invoke-Check {
    param([scriptblock] $Command)
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "Check failed: $Command"
    }
}

Push-Location (Join-Path $PSScriptRoot '..')
try {
    # Separate artifacts avoid replacing an executable held by a running API.
    Invoke-Check { dotnet test --artifacts-path "$env:TEMP/appointly-phase6-checks" }
    Invoke-Check { npm --prefix frontend run lint }
    Invoke-Check { npm --prefix frontend test }
    # Builds production assets, starts its own preview, and closes it afterward.
    Invoke-Check { npm --prefix frontend run test:browser }
    Invoke-Check { git diff --check }
}
finally {
    Pop-Location
}
