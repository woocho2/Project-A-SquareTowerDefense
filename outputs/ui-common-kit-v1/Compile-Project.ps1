$ErrorActionPreference = 'Stop'
$taskRepo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskBuild = Join-Path $PSScriptRoot 'compile'
New-Item -ItemType Directory -Path $taskBuild -Force | Out-Null
$taskItems = [Collections.Generic.List[string]]::new()
$taskReferences = @{}
foreach ($taskFolder in @('Assets/3. Scripts', 'Assets/Editor')) {
    foreach ($taskFile in Get-ChildItem -LiteralPath (Join-Path $taskRepo $taskFolder) -Filter '*.cs' -Recurse) {
        $taskEscaped = [Security.SecurityElement]::Escape($taskFile.FullName)
        $taskItems.Add("<Compile Include=`"$taskEscaped`" />")
    }
}
foreach ($taskProjectName in @('Assembly-CSharp.csproj', 'Assembly-CSharp-Editor.csproj')) {
    $taskOriginal = [xml]([IO.File]::ReadAllText((Join-Path $taskRepo $taskProjectName)))
    foreach ($taskReference in $taskOriginal.Project.ItemGroup.Reference) {
        if ($taskReference.Include -match '^(System\.|Microsoft\.|netstandard$|mscorlib$|Assembly-CSharp$)') { continue }
        $taskHint = [string]$taskReference.HintPath
        if (!$taskHint) { continue }
        if (![IO.Path]::IsPathRooted($taskHint)) { $taskHint = Join-Path $taskRepo $taskHint }
        if (!(Test-Path -LiteralPath $taskHint)) { continue }
        $taskReferences[[string]$taskReference.Include] = $taskHint
    }
}
foreach ($taskReference in $taskReferences.GetEnumerator()) {
    $taskName = [Security.SecurityElement]::Escape($taskReference.Key)
    $taskHint = [Security.SecurityElement]::Escape($taskReference.Value)
    $taskItems.Add("<Reference Include=`"$taskName`"><HintPath>$taskHint</HintPath></Reference>")
}
$taskDefines = [Security.SecurityElement]::Escape([string]$taskOriginal.Project.PropertyGroup.DefineConstants)
$taskXml = @"
<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup>
<TargetFramework>net10.0</TargetFramework>
<EnableDefaultCompileItems>false</EnableDefaultCompileItems>
<DefineConstants>$taskDefines</DefineConstants>
<NoWarn>0649;0169;0414;0618</NoWarn>
<AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
<ItemGroup>
$($taskItems -join "`n")
</ItemGroup>
</Project>
"@
$taskProject = Join-Path $taskBuild 'ProjectCompile.csproj'
[IO.File]::WriteAllText($taskProject, $taskXml, [Text.UTF8Encoding]::new($false))
& dotnet build $taskProject --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Project compile failed' }
