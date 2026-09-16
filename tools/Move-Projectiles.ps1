$files = @(
    "Projectile_Icicle_Small",
    "Projectile_Zeus_Thunderbolt",
    "Projectile_Wind_Orb",
    "Projectile_Earth_Orb",
    "Projectile_Light_Laser",
    "Projectile_Dark_Orb"
)

foreach ($f in $files) {
    $srcPng = "Assets/4. DotAsset/$f.png"
    $srcMeta = "Assets/4. DotAsset/$f.png.meta"
    $dstPng = "Assets/4. DotAsset/3. Projectile/$f.png"
    $dstMeta = "Assets/4. DotAsset/3. Projectile/$f.png.meta"

    if (Test-Path $srcPng) {
        Move-Item -Path $srcPng -Destination $dstPng -Force
        Write-Output "Moved $srcPng -> $dstPng"
    }
    if (Test-Path $srcMeta) {
        Move-Item -Path $srcMeta -Destination $dstMeta -Force
        Write-Output "Moved $srcMeta -> $dstMeta"
    }
}
