$dir = 'd:\MyGitHub\ProjectA\Project-A-SquareTowerDefense\Assets\4. DotAsset\3. Projectile\NewElementalBullets'
$template = Get-Content 'd:\MyGitHub\ProjectA\Project-A-SquareTowerDefense\Assets\4. DotAsset\3. Projectile\Debuff\Color\Projectile_Fire.png.meta' -Raw
$list = @('Fire','Ice','Electricity','Wind','Earth','Light','Darkness')
for ($i = 0; $i -lt $list.Count; $i++) {
    $name = $list[$i]
    $guid = "9a07010000000000000000000000000" + ($i + 1)
    $meta = $template -replace 'guid: [0-9a-fA-F]+', "guid: $guid"
    Set-Content (Join-Path $dir "Projectile_$name.png.meta") $meta -NoNewline
    Write-Output "Created meta for Projectile_$name"
}
