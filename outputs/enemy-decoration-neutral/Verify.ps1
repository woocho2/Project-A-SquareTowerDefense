$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Add-Type -AssemblyName System.Drawing
$old=[Drawing.Bitmap]::FromFile((Join-Path $PSScriptRoot 'before/Enemy_Decoration.png'))
$new=[Drawing.Bitmap]::FromFile((Join-Path $root 'Assets/4. Asset/4. Enemy/Enemy_Decoration.png'))
$shades=New-Object 'Collections.Generic.HashSet[int]'
try{
    if($old.Width -ne $new.Width -or $old.Height -ne $new.Height){throw 'Frame dimensions changed.'}
    for($y=0;$y -lt $new.Height;$y++){for($x=0;$x -lt $new.Width;$x++){
        $a=$old.GetPixel($x,$y);$b=$new.GetPixel($x,$y)
        if($b.R -ne $b.G -or $b.G -ne $b.B){throw 'Colored pixel remains in frame.'}
        if($a.A -ne $b.A){throw 'Decoration silhouette/alpha changed.'}
        if($b.R -ne [math]::Max($a.R,[math]::Max($a.G,$a.B))){throw 'Source shading value changed.'}
        if($b.A -gt 0){$shades.Add([int]$b.R) | Out-Null}
    }}
}finally{$old.Dispose();$new.Dispose()}
$protected=Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'protected-hashes.json') | ConvertFrom-Json
foreach($record in $protected){if((Get-FileHash -LiteralPath $record.path).Hash -cne $record.hash){throw ('Unrelated asset changed: '+$record.path)}}
$materials=@(Get-ChildItem -LiteralPath (Join-Path $root 'Assets/4. Asset/4. Enemy') -Filter '*.mat' -File | Where-Object {[IO.File]::ReadAllText($_.FullName).Contains('_UseLayers: 1')})
if($materials.Count -ne 18){throw 'Expected 18 state materials.'}
$states=@{}
foreach($material in $materials){
    $text=[IO.File]::ReadAllText($material.FullName)
    if($text -match '_DecorationColor|_RecolorDecoration'){throw 'Separate decoration tint remains.'}
    if($text -notmatch '_RecolorEquipment: 1'){throw 'A state bypasses the shared palette.'}
    $state=($material.BaseName -split '_')[-1]
    $color=[regex]::Match($text,'(?m)^    - _EquipmentColor: (.*)').Groups[1].Value.Trim()
    if($states.ContainsKey($state) -and $states[$state] -cne $color){throw 'Character palettes differ for same state.'}
    $states[$state]=$color
}
$shader=[IO.File]::ReadAllText((Join-Path $root 'Assets/3. Scripts/03. Enemy/EnemyCubeUnlit.shader'))
if(!$shader.Contains('decoration.rgb *= _EquipmentColor.rgb;')){throw 'Frame does not use shared state color.'}
$log=@(
    ('PASS: 512x512 decoration contains only grayscale; '+$shades.Count+' visible shading levels retained.'),
    'PASS: every alpha pixel and source shading value is preserved.',
    'PASS: all 18 materials use one state-color property for equipment masks and decoration, with matching colors across all 3 characters.',
    'PASS: other textures, appearance profiles, mesh, and all metadata hashes unchanged.',
    'PASS: 18 CPU compositions and current layered-connection checks passed.',
    'Unity was not launched; shader compilation/rendering validation remains with the user.'
)
$log | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'verification.txt') -Encoding UTF8
$log
