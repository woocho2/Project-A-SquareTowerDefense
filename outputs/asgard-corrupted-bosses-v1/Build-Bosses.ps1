$ErrorActionPreference = 'Stop'
# One canonical portrait per boss prevents state variants from drifting.
& (Join-Path $PSScriptRoot 'aligned-portraits/Align-Portraits.ps1')
foreach ($boss in @('Brunhild', 'Loki')) {
    foreach ($name in @("Enemy_Fallen${boss}_States_Atlas_v1.png", "${boss}-portraits-packed.png")) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot "aligned-portraits/$name") -Destination (Join-Path $PSScriptRoot $name) -Force
    }
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'aligned-portraits/preview.png') -Destination (Join-Path $PSScriptRoot 'overview.png') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'aligned-portraits/verification.txt') -Destination (Join-Path $PSScriptRoot 'verification.txt') -Force