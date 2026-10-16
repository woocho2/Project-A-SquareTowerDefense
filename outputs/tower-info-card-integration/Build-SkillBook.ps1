$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$document = [IO.File]::ReadAllText((Join-Path $repoRoot 'Assets/20. 통합 기획서.txt'))
$skillNames = @(
    @('기사의 검','신성의 검','저주받은 검','망령의 검','엑스칼리버','이십사수매화검법'),
    @('전쟁의 노래','광란의 노래','선고','처형','강타','쪼개기'),
    @('요정의 활','정령의 활','락 온','핀 포인트','화살비','은화살'),
    @('카우치','랜스 차지','출혈','과다출혈','선두 지휘','뽑아찢기'),
    @('수호의 방패','가디언의 방패','방패벽','저지선','방패 돌격','파쇄'),
    @('제련','세공','원석','금맥','100T 해머','망치질'),
    @('불씨','불길','화상','연소','메테오','화염구'),
    @('물결','해류','젖음','침수','쓰나미','급류'),
    @('성장','만개','진흙','석화','대지의 분노','씨뿌리기'),
    @('순풍','질풍','역풍','난기류','칼바람','폭풍'),
    @('냉기','한기','동상','저체온증','블리자드','스노우볼'),
    @('충전','과충전','누전','감전','라이트닝 볼텍스','과부하'),
    @('광채','광휘','잔영','잔상','프리즘','도깨비불'),
    @('포식','폭식','잠식','침식','블랙홀','심연')
)
function Clean-Paragraph([string] $text) {
    $lines = $text -split '\r?\n' | ForEach-Object { $_.Trim() } | Where-Object {
        $_ -notmatch '^={5,}' -and $_ -notmatch '^작업은' -and $_ -notmatch '^틀은 다른' -and $_ -notmatch '^4-\d+\.'
    }
    return (($lines -join "`n") -replace '\n{3,}', "`n`n").Trim()
}
$entries = [Collections.Generic.List[object]]::new()
for ($emblem = 1; $emblem -le 14; $emblem++) {
    $section = $emblem + 2
    $heading = [regex]::Match($document, "(?m)^4-$section\. ")
    if (!$heading.Success) { throw "Missing emblem chapter $emblem" }
    $tail = $document.Substring($heading.Index + $heading.Length)
    $nextHeading = [regex]::Match($tail, '(?m)^\d+(?:-\d+)?\. ')
    $chapter = if ($nextHeading.Success) { $tail.Substring(0, $nextHeading.Index) } else { $tail }
    $groups = [regex]::Matches($chapter, '(?m)^(광역 타워|단일 타워|버프 타워|디버프 타워)(?:의| —| \(|\r?$)')
    if ($groups.Count -ne 4) { throw "Expected four attack blocks for emblem $emblem, got $($groups.Count)" }
    for ($attack = 0; $attack -lt 4; $attack++) {
        $start = $groups[$attack].Index
        $end = if ($attack -lt 3) { $groups[$attack+1].Index } else { $chapter.Length }
        $block = $chapter.Substring($start, $end - $start)
        $firstBreak = $block.IndexOf("`n")
        $body = if ($firstBreak -ge 0) { $block.Substring($firstBreak+1) } else { $block }
        if ($attack -lt 2) {
            $name1 = if ($attack -eq 0) { '광역 공격' } else { '단일 공격' }
            $name2 = $skillNames[$emblem-1][4+$attack]
            $description1 = if ($attack -eq 0) {
                'Action 턴마다 사거리 안에서 우선순위로 고른 적의 타일을 공격합니다. 착탄 범위의 모든 적에게 Power 피해를 AttackCount회 줍니다. Mastery는 착탄 범위를 뜻합니다.'
            } else {
                'Action 턴마다 사거리 안에서 우선순위로 고른 적을 공격합니다. 한 번 행동할 때 AttackCount × 발 수만큼 Power 피해를 줍니다. Mastery는 한 번 공격의 발 수를 뜻합니다.'
            }
            $description2 = Clean-Paragraph $body
        } else {
            $offset = if ($attack -eq 2) { 0 } else { 2 }
            $name1 = $skillNames[$emblem-1][$offset]
            $name2 = $skillNames[$emblem-1][$offset+1]
            $first = [regex]::Match($body, '(?m)^\s*스킬 1\s*\(')
            $second = [regex]::Match($body, '(?m)^\s*스킬 2\s*\(')
            if ($first.Success -and $second.Success -and $second.Index -gt $first.Index) {
                $common = $body.Substring(0, $first.Index)
                $description1 = Clean-Paragraph ($common + $body.Substring($first.Index, $second.Index - $first.Index))
                $description2 = Clean-Paragraph ($common + $body.Substring($second.Index))
            } else {
                $description1 = Clean-Paragraph $body
                $description2 = $description1
            }
        }
        if (!$description1 -or !$description2) { throw "Empty skill description: $emblem/$attack" }
        $entries.Add([ordered]@{ emblem=$emblem; attack=$attack; name1=$name1; name2=$name2; description1=$description1; description2=$description2 })
    }
}
$json = @{ entries = $entries.ToArray() } | ConvertTo-Json -Depth 5
[IO.File]::WriteAllText((Join-Path $repoRoot 'Assets/Resources/TowerInfoCardSkills.json'), $json, [Text.UTF8Encoding]::new($false))
Write-Output "Created $($entries.Count) tower skill descriptions from the current design document."
