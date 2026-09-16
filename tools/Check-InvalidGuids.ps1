$files = Get-ChildItem -Path "Assets" -Filter "*.meta" -Recurse

foreach ($f in $files) {
    $lines = Get-Content $f.FullName
    foreach ($line in $lines) {
        if ($line -match "^guid:\s*([a-zA-Z0-9]+)") {
            $guid = $matches[1]
            if ($guid.Length -ne 32 -or $guid -notmatch "^[0-9a-fA-F]{32}$") {
                Write-Output "INVALID GUID in $($f.FullName): $guid"
            }
        }
    }
}
Write-Output "GUID scan complete."
