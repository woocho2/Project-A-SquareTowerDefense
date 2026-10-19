$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$assets=Join-Path $root 'Assets'
$surfaceGuid='f43424de21bfacd130bf5c0d3f0d9400'
$panelGuid='5490e0460ebb7953510c8ca8aa97ad75'
$imageGuid='fe87c0e1cc204ed48ad3b37840f39efc'
$frameNameKorean=-join [char[]]@(0xD504,0xB808,0xC784)
$blockPattern='(?ms)^--- !u!(?<type>\d+) &(?<id>-?\d+)[^\r\n]*\r?\n.*?(?=^--- !u!|\z)'
$plans=@()
foreach($file in Get-ChildItem -LiteralPath $assets -Recurse -File | Where-Object {$_.Extension -in '.prefab','.unity','.asset'}) {
    $text=[IO.File]::ReadAllText($file.FullName)
    if(-not $text.Contains($surfaceGuid) -and -not $text.Contains($panelGuid)){continue}
    $blocks=[regex]::Matches($text,$blockPattern)
    $byId=@{};$images=@{}
    foreach($block in $blocks) {
        $byId[$block.Groups['id'].Value]=$block.Value
        if($block.Value -match "m_Script:.*guid: $imageGuid") {
            $go=[regex]::Match($block.Value,'m_GameObject: \{fileID: (-?\d+)\}').Groups[1].Value
            $images[$go]=$block.Groups['id'].Value
        }
    }
    $result=$text;$removed=@();$kept=@();$panelCount=0
    foreach($block in $blocks) {
        if($block.Value -notmatch "m_Script:.*guid: $surfaceGuid"){continue}
        $id=$block.Groups['id'].Value
        $go=[regex]::Match($block.Value,'m_GameObject: \{fileID: (-?\d+)\}').Groups[1].Value
        $name=[regex]::Match($byId[$go],'(?m)^  m_Name: (.+)\r?$').Groups[1].Value.Trim()
        $role=[regex]::Match($block.Value,'(?m)^  colorRole: (\d+)').Groups[1].Value
        $isFrame=$role -eq '3' -or $name -match '(?i)(^|[_\s])Frame(?=[_\s]|$)' -or $name.Contains($frameNameKorean)
        if($isFrame) {
            $kept+=@{name=$name;componentID=$id}
            continue
        }
        if(-not $images.ContainsKey($go)){throw "Background Image missing: $($file.FullName) / $name"}
        $removed+=@{name=$name;componentID=$id;imageID=$images[$go]}
        $result=$result.Replace($block.Value,'')
        $result=[regex]::Replace($result,"(?m)^  - component: \{fileID: $id\}\r?\n",'')
    }
    foreach($block in $blocks) {
        if($block.Value -notmatch "m_Script:.*guid: $panelGuid"){continue}
        $panel=$block.Value
        foreach($entry in $removed) {
            $panel=$panel.Replace(('  background: {fileID: '+$entry.componentID+'}'),('  background: {fileID: '+$entry.imageID+'}'))
        }
        $panel=[regex]::Replace($panel,'(?m)^  backgroundRole: [^\r\n]*\r?\n','')
        if($panel -cne $block.Value){$result=$result.Replace($block.Value,$panel);$panelCount++}
    }
    foreach($entry in $removed) {
        if($result -match ('\{fileID:\s*'+[regex]::Escape($entry.componentID)+'\}')){throw "Removed component still referenced: $($file.FullName) / $($entry.componentID)"}
    }
    # Graphic properties, layout and object hierarchy must remain byte-identical.
    foreach($property in @('m_Color','m_Sprite','m_Type','m_PixelsPerUnitMultiplier','m_SizeDelta','m_AnchoredPosition','m_AnchorMin','m_AnchorMax','m_Children','m_Father')) {
        $pattern='(?m)^  '+$property+':[^\r\n]*'
        $before=([regex]::Matches($text,$pattern)|ForEach-Object {$_.Value}) -join "`n"
        $after=([regex]::Matches($result,$pattern)|ForEach-Object {$_.Value}) -join "`n"
        if($before -cne $after){throw "Visual/layout property changed: $property"}
    }
    $plans+=@{path=$file.FullName;original=$text;result=$result;removed=$removed;kept=$kept;updatedPanelCount=$panelCount}
}
# Check cross-file references to removed prefab components before writing any assets.
foreach($plan in $plans) {
    $meta=[IO.File]::ReadAllText($plan.path+'.meta')
    $guid=[regex]::Match($meta,'(?m)^guid: (\w+)').Groups[1].Value
    foreach($entry in $plan.removed) {
        $reference='\{fileID:\s*'+[regex]::Escape($entry.componentID)+',\s*guid:\s*'+$guid+'(?:,|\})'
        foreach($file in Get-ChildItem -LiteralPath $assets -Recurse -File | Where-Object {$_.Extension -in '.prefab','.unity','.asset'}) {
            $other=@($plans | Where-Object {$_.path -eq $file.FullName})
            $content=if($other.Count -gt 0){$other[0].result}else{[IO.File]::ReadAllText($file.FullName)}
            if($content -match $reference){throw "External removed-component reference: $($file.FullName)"}
        }
    }
}
$backup=Join-Path $PSScriptRoot 'before'
if(Test-Path -LiteralPath $backup){throw 'Component cleanup backup already exists'}
$utf8=[Text.UTF8Encoding]::new($false)
$report=@()
foreach($plan in $plans) {
    $rel=$plan.path.Substring($root.Length+1)
    if($plan.result -cne $plan.original) {
        $dest=Join-Path $backup ($rel+'.txt')
        New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($dest)) -Force | Out-Null
        Copy-Item -LiteralPath $plan.path -Destination $dest
        [IO.File]::WriteAllText($plan.path,$plan.result,$utf8)
    }
    $report+=@{path=$rel;removed=$plan.removed;keptFrames=$plan.kept;updatedPanels=$plan.updatedPanelCount;visualPropertiesUnchanged=$true}
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verification.json'),(ConvertTo-Json -InputObject @($report) -Depth 7),$utf8)
$report|ForEach-Object {"$($_.path): removed $($_.removed.Count), kept frames $($_.keptFrames.Count), updated panels $($_.updatedPanels)"}
