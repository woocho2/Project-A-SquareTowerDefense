Add-Type -AssemblyName System.Drawing

$img = [System.Drawing.Image]::FromFile("Assets/4. DotAsset/3. Projectile/NewElementalBullets/Projectile_Ice.png")
Write-Output "Image size: $($img.Width) x $($img.Height)"
$img.Dispose()
