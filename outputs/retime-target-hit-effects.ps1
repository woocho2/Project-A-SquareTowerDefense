$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$directory = Join-Path $root 'Assets/4. Asset/6. Effect'
$files = @(Get-ChildItem -LiteralPath $directory -File -Filter '*Target*.anim' | Sort-Object Name)
if ($files.Count -ne 14) { throw 'Expected exactly 14 target hit clips.' }
$backup = Join-Path $PSScriptRoot ('target-hit-effect-1s-backup-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
$curveRegex = [regex]::new('(?ms)(^  m_PPtrCurves:\r?\n  - serializedVersion: 2\r?\n    curve:\r?\n)(.*?)(^    attribute: m_Sprite\r?\n)')
$mappingRegex = [regex]::new('(?ms)(^    pptrCurveMapping:\r?\n)(.*?)(^  m_AnimationClipSettings:)')
$plans = @()
foreach ($file in $files) {
    $original = [IO.File]::ReadAllText($file.FullName)
    $curve = $curveRegex.Match($original)
    $keys = [regex]::Matches($curve.Groups[2].Value, '(?m)^    - time: ([^\r\n]+)\r?\n      value: (\{[^\r\n]+\})\r?\n')
    if (!$curve.Success -or $keys.Count -ne 30 -or $original -notmatch 'm_SampleRate: 12\r?\n' -or $original -notmatch 'm_StopTime: 2.5\r?\n') { throw "Unexpected clip format: $($file.Name)" }
    if ([regex]::Matches($original, 'functionName:').Count -ne 1 -or $original -notmatch 'functionName: DestroyEffect') { throw "Unexpected events: $($file.Name)" }
    foreach ($field in @('m_RotationCurves','m_CompressedRotationCurves','m_EulerCurves','m_PositionCurves','m_ScaleCurves','m_FloatCurves','m_EditorCurves','m_EulerEditorCurves')) {
        if ($original -notmatch ($field + ': \[\]')) { throw "Additional curves in $($file.Name): $field" }
    }
    $newline = if ($original.Contains("`r`n")) { "`r`n" } else { "`n" }
    $newKeys = ''; $newMapping = ''; $indices = @(); $sprites = @()
    for ($i = 0; $i -lt 12; $i++) {
        $index = [int][Math]::Round($i * 29.0 / 11.0)
        $indices += $index
        $sprite = $keys[$index].Groups[2].Value
        $sprites += $sprite
        $time = ($i / 12.0).ToString('0.00000000', [Globalization.CultureInfo]::InvariantCulture)
        $newKeys += '    - time: ' + $time + $newline + '      value: ' + $sprite + $newline
        $newMapping += '    - ' + $sprite + $newline
    }
    $changed = $curveRegex.Replace($original, {param($m) $m.Groups[1].Value + $newKeys + $m.Groups[3].Value}, 1)
    $changed = $mappingRegex.Replace($changed, {param($m) $m.Groups[1].Value + $newMapping + $m.Groups[3].Value}, 1)
    $changed = [regex]::Replace($changed, '(?m)^(    m_StopTime: )2\.5(?=\r?$)', '${1}1')
    $changed = [regex]::Replace($changed, '(?m)^(  - time: )2\.5(?=\r?\n    functionName: DestroyEffect)', '${1}1')
    $plans += [pscustomobject]@{File=$file;Original=$original;Changed=$changed;Indices=$indices;Sprites=$sprites}
}
[void](New-Item -ItemType Directory -Path $backup)
$records = @()
foreach ($plan in $plans) {
    $relative = $plan.File.FullName.Substring($root.Length + 1)
    $saved = Join-Path $backup $relative
    [void](New-Item -ItemType Directory -Path (Split-Path $saved -Parent) -Force)
    Copy-Item -LiteralPath $plan.File.FullName -Destination $saved
    Copy-Item -LiteralPath ($plan.File.FullName + '.meta') -Destination ($saved + '.meta')
    $records += [pscustomobject]@{Path=$relative;Hash=(Get-FileHash -LiteralPath $saved -Algorithm SHA256).Hash;SourceFrameIndices=$plan.Indices}
}
$records | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $backup 'manifest.json') -Encoding UTF8
foreach ($plan in $plans) {
    [IO.File]::WriteAllText($plan.File.FullName, $plan.Changed, [Text.UTF8Encoding]::new($false))
    $actual = [IO.File]::ReadAllText($plan.File.FullName)
    $actualKeys = [regex]::Matches($curveRegex.Match($actual).Groups[2].Value, '(?m)^    - time: ([^\r\n]+)\r?\n      value: (\{[^\r\n]+\})\r?\n')
    $actualMapping = [regex]::Matches($mappingRegex.Match($actual).Groups[2].Value, '(?m)^    - (\{[^\r\n]+\})')
    if ($actualKeys.Count -ne 12 -or $actualMapping.Count -ne 12 -or $actual -notmatch 'm_StopTime: 1\r?\n' -or $actual -notmatch 'm_Events:\r?\n  - time: 1\r?\n    functionName: DestroyEffect') { throw "Verification failed: $($plan.File.Name)" }
    for ($i = 0; $i -lt 12; $i++) {
        if ($actualKeys[$i].Groups[2].Value -ne $plan.Sprites[$i] -or $actualMapping[$i].Groups[1].Value -ne $plan.Sprites[$i]) { throw 'Sprite mapping mismatch.' }
    }
    $reverse = $curveRegex.Replace($actual, {param($m) $curveRegex.Match($plan.Original).Value}, 1)
    $reverse = $mappingRegex.Replace($reverse, {param($m) $mappingRegex.Match($plan.Original).Value}, 1)
    $reverse = $reverse.Replace('    m_StopTime: 1' + "`r`n", '    m_StopTime: 2.5' + "`r`n").Replace('    m_StopTime: 1' + "`n", '    m_StopTime: 2.5' + "`n")
    $reverse = [regex]::Replace($reverse, '(?m)^(  - time: )1(?=\r?\n    functionName: DestroyEffect)', '${1}2.5')
    if ($reverse -cne $plan.Original) { throw "Unintended fields changed: $($plan.File.Name)" }
}
[pscustomobject]@{ChangedClips=$plans.Count;Frames=12;FPS=12;DurationSeconds=1;Backup=$backup} | Format-List
