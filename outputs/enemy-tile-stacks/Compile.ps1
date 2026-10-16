$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$checkRoot = Join-Path $PSScriptRoot 'compile'
New-Item -ItemType Directory -Path $checkRoot -Force | Out-Null
$original = [xml]([IO.File]::ReadAllText((Join-Path $repoRoot 'Assembly-CSharp.csproj')))
$items = [Collections.Generic.List[string]]::new()
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $repoRoot 'Assets/3. Scripts') -Filter '*.cs' -Recurse) {
    $path = [Security.SecurityElement]::Escape($file.FullName)
    $items.Add("<Compile Include=`"$path`" />")
}
foreach ($reference in $original.Project.ItemGroup.Reference) {
    if ($reference.Include -match '^(System\.|Microsoft\.|netstandard$|mscorlib$)') { continue }
    $hintPath = [string]$reference.HintPath
    if (!$hintPath) { continue }
    if (![IO.Path]::IsPathRooted($hintPath)) { $hintPath = Join-Path $repoRoot $hintPath }
    if (!(Test-Path -LiteralPath $hintPath)) { continue }
    $path = [Security.SecurityElement]::Escape($hintPath)
    $name = [Security.SecurityElement]::Escape([string]$reference.Include)
    $items.Add("<Reference Include=`"$name`"><HintPath>$path</HintPath></Reference>")
}
$firstpass = [Security.SecurityElement]::Escape((Join-Path $repoRoot 'Library/ScriptAssemblies/Assembly-CSharp-firstpass.dll'))
$items.Add("<Reference Include=`"Assembly-CSharp-firstpass`"><HintPath>$firstpass</HintPath></Reference>")
$defines = [Security.SecurityElement]::Escape([string]$original.Project.PropertyGroup.DefineConstants)
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup>
<TargetFramework>net10.0</TargetFramework>
<EnableDefaultCompileItems>false</EnableDefaultCompileItems>
<DefineConstants>$defines</DefineConstants>
<NoWarn>0649;0169;0414;0618</NoWarn>
<AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
<ItemGroup>
$($items -join "`n")
</ItemGroup>
</Project>
"@
$projectPath = Join-Path $checkRoot 'RuntimeCompile.csproj'
[IO.File]::WriteAllText($projectPath, $project, [Text.UTF8Encoding]::new($false))
& dotnet build $projectPath --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Runtime compile failed' }
