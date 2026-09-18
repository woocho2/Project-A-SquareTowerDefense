param(
    [string]$InputPath = "Assets/Resources/TowerDataCSV.csv",
    [string]$OutputPath = "Assets/TowerDataCSV_BalanceDraft.csv"
)

$ErrorActionPreference = "Stop"
$invariant = [System.Globalization.CultureInfo]::InvariantCulture

$patternStats = @{
    1  = @{ Power = 5;  Range = 2;  Action = 6;  AttackCount = 3;  CriticalRate = 5;  CriticalDamage = 5 }
    2  = @{ Power = 2;  Range = 10; Action = 6;  AttackCount = 10; CriticalRate = 6;  CriticalDamage = 2 }
    3  = @{ Power = 4;  Range = 2;  Action = 1;  AttackCount = 2;  CriticalRate = 5;  CriticalDamage = 8 }
    4  = @{ Power = 3;  Range = 7;  Action = 6;  AttackCount = 7;  CriticalRate = 10; CriticalDamage = 1 }
    5  = @{ Power = 10; Range = 1;  Action = 1;  AttackCount = 1;  CriticalRate = 1;  CriticalDamage = 10 }
    6  = @{ Power = 8;  Range = 1;  Action = 1;  AttackCount = 1;  CriticalRate = 3;  CriticalDamage = 8 }
    7  = @{ Power = 7;  Range = 7;  Action = 10; AttackCount = 1;  CriticalRate = 6;  CriticalDamage = 2 }
    8  = @{ Power = 10; Range = 7;  Action = 2;  AttackCount = 10; CriticalRate = 8;  CriticalDamage = 1 }
    9  = @{ Power = 2;  Range = 7;  Action = 8;  AttackCount = 10; CriticalRate = 3;  CriticalDamage = 4 }
    10 = @{ Power = 1;  Range = 7;  Action = 9;  AttackCount = 3;  CriticalRate = 5;  CriticalDamage = 2 }
    11 = @{ Power = 9;  Range = 5;  Action = 2;  AttackCount = 2;  CriticalRate = 8;  CriticalDamage = 5 }
    12 = @{ Power = 1;  Range = 10; Action = 10; AttackCount = 1;  CriticalRate = 8;  CriticalDamage = 1 }
    13 = @{ Power = 8;  Range = 3;  Action = 5;  AttackCount = 5;  CriticalRate = 5;  CriticalDamage = 8 }
}

$colorStats = @{
    Splash = @{ Power = 3.5; CriticalRate = 2.5; CriticalDamage = 7.0; Speed = 5 }
    Target = @{ Power = 9.5; CriticalRate = 7.5; CriticalDamage = 1.5; Speed = 10 }
    Buff   = @{ Power = 5.0; CriticalRate = 0.0; CriticalDamage = 0.0; Speed = 0 }
    Debuff = @{ Power = 5.0; CriticalRate = 0.0; CriticalDamage = 0.0; Speed = 0 }
}

# Each promotion has one clear growth focus.
# Silver: Action, Gold: Power/Critical, Mythril: AttackCount,
# Diamond: Action/Ability/Duration/Signature area or arrows.
$powerTierMultiplier = @(0.0, 1.0, 1.0, 2.0, 2.0, 2.0)
$criticalTierMultiplier = @(0.0, 1.0, 1.0, 2.0, 2.0, 2.0)
$targetAttackCountBase = @(0, 1, 1, 1, 4, 4)
$splashAttackCountBase = @(0, 1, 1, 1, 2, 2)
$radiusByTier = @(0, 1, 1, 1, 1, 3)

function Get-PatternFactor([int]$score) { return 0.5 + ($score / 10.0) }

function Get-Range([string]$attackType, [int]$score, [int]$tier) {
    $baseRange = if ($score -le 2) { 1 } elseif ($score -le 5) { 2 } elseif ($score -le 8) { 3 } else { 4 }
    $utilityTierBonus = if (($attackType -eq "Buff" -or $attackType -eq "Debuff") -and $tier -ge 4) { 1 } else { 0 }
    return $baseRange + $utilityTierBonus
}

function Get-Action([int]$score, [int]$tier) {
    $baseAction = if ($score -ge 10) { 1 } elseif ($score -ge 8) { 2 } elseif ($score -ge 5) { 3 } elseif ($score -ge 3) { 4 } else { 5 }
    $tierReduction = if ($tier -ge 5) { 2 } elseif ($tier -ge 2) { 1 } else { 0 }
    return [Math]::Max(1, $baseAction - $tierReduction)
}

function Get-AttackCountFactor([int]$score) {
    if ($score -le 2) { return 0.50 }
    if ($score -le 4) { return 0.625 }
    if ($score -le 6) { return 0.75 }
    if ($score -le 8) { return 0.875 }
    return 1.00
}

function Get-AttackCount([string]$attackType, [int]$score, [int]$tier) {
    if ($attackType -eq "Buff" -or $attackType -eq "Debuff") { return 1 }
    if ($tier -le 3) { return 1 }
    $baseCount = if ($attackType -eq "Target") { $targetAttackCountBase[$tier] } else { $splashAttackCountBase[$tier] }
    return [Math]::Max(1, [int][Math]::Round($baseCount * (Get-AttackCountFactor $score), 0, [MidpointRounding]::AwayFromZero))
}

function Get-CriticalRate([string]$attackType, [int]$score, [int]$tier) {
    $color = $colorStats[$attackType]
    if ($color.CriticalRate -le 0) { return 0.0 }
    return [Math]::Min(0.75, 0.02 * $color.CriticalRate * (Get-PatternFactor $score) * $criticalTierMultiplier[$tier])
}

function Get-CriticalDamage([string]$attackType, [int]$score, [int]$tier) {
    $color = $colorStats[$attackType]
    if ($color.CriticalDamage -le 0) { return 0.0 }
    return 0.10 * $color.CriticalDamage * (Get-PatternFactor $score) * $criticalTierMultiplier[$tier]
}

function Get-ExpectedCriticalMultiplier([double]$rate, [double]$damage) {
    # Normal damage is 1x; critical damage is (2 + CriticalDamage)x.
    return 1.0 + ($rate * (1.0 + $damage))
}

function Get-Duration([string]$attackType, [int]$tier) {
    switch ($attackType) {
        "Splash" { return @(0, 5, 5, 5, 5, 2)[$tier] }
        "Target" { return @(0, 8, 8, 8, 8, 4)[$tier] }
        default  { return @(0, 3, 3, 3, 3, 6)[$tier] }
    }
}

function Get-AbilityValue([string]$attackType, [int]$tier) {
    if ($attackType -eq "Splash" -or $attackType -eq "Target") {
        return @(0.0, 1.5, 1.5, 1.5, 1.5, 4.0)[$tier]
    }
    # For utility towers AbilityValue is a skill stack requirement: lower is better.
    return @(0, 10, 10, 10, 10, 3)[$tier]
}

function Format-Number([double]$value, [int]$digits = 4) {
    return $value.ToString("0." + ("#" * $digits), $invariant)
}

$inputLines = [System.IO.File]::ReadAllLines(
    (Join-Path (Get-Location) $InputPath),
    [System.Text.Encoding]::UTF8)
$rows = $inputLines | ConvertFrom-Csv
$outputLines = [System.Collections.Generic.List[string]]::new()
$outputLines.Add(($rows[0].PSObject.Properties.Name -join ","))

foreach ($row in $rows) {
    $id = [int]$row.ID
    $tier = [int][Math]::Floor($id / 1000)
    $pattern = $id % 100
    $attackType = $row.attackType
    $patternStat = $patternStats[$pattern]
    $colorStat = $colorStats[$attackType]
    if ($null -eq $patternStat -or $null -eq $colorStat) { throw "Unsupported tower ID or attack type. ID=$id, AttackType=$attackType" }

    $action = Get-Action $patternStat.Action $tier
    $attackCount = Get-AttackCount $attackType $patternStat.AttackCount $tier
    $additionalHitCount = if ($attackType -eq "Target") { $radiusByTier[$tier] } else { 0 }
    $splashRadius = if ($attackType -eq "Splash") { $radiusByTier[$tier] } else { 0 }
    $criticalRate = Get-CriticalRate $attackType $patternStat.CriticalRate $tier
    $criticalDamage = Get-CriticalDamage $attackType $patternStat.CriticalDamage $tier

    $bronzePower = 2.0 * ($colorStat.Power / 5.0) * (Get-PatternFactor $patternStat.Power)
    $power = $bronzePower * $powerTierMultiplier[$tier]

    $row.attackDamage = Format-Number $power 3
    $row.attackRange = Get-Range $attackType $patternStat.Range $tier
    $row.Action = $action
    $row.attackCount = $attackCount
    $row.SplashRadius = $splashRadius
    $row.AdditionalHitCount = $additionalHitCount
    $row.IsCritical = "FALSE"
    $row.criticalRate = Format-Number $criticalRate 4
    $row.criticalDamage = Format-Number $criticalDamage 4
    $row.duration = Get-Duration $attackType $tier
    $row.abilityValue = Format-Number (Get-AbilityValue $attackType $tier) 2
    $row.Speed = $colorStat.Speed

    $values = foreach ($property in $row.PSObject.Properties) { [string]$property.Value }
    $outputLines.Add(($values -join ","))
}

$outputDirectory = Split-Path -Parent $OutputPath
if ($outputDirectory -and -not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

[System.IO.File]::WriteAllLines(
    (Join-Path (Get-Location) $OutputPath),
    $outputLines,
    [System.Text.UTF8Encoding]::new($false))

Write-Output "Generated $($rows.Count) rows: $OutputPath"
