$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
# Collect current source files and the installed Unity assembly references without opening Unity.
& (Join-Path $repo 'outputs/enemy-tile-stacks/Compile.ps1')
if($LASTEXITCODE -ne 0){throw 'Runtime editor-define compilation failed'}
$runtime=Join-Path $repo 'outputs/enemy-tile-stacks/compile/RuntimeCompile.csproj'
$text=[IO.File]::ReadAllText($runtime)
$extra=@('Assets/Editor/SharedBoardVisualsEditor.cs','Assets/Editor/TileManagerEditor.cs') | ForEach-Object {
    '<Compile Include="'+[Security.SecurityElement]::Escape((Join-Path $repo $_))+'" />'
}
$editor=$text.Replace('</ItemGroup>',(($extra -join "`n")+"`n</ItemGroup>"))
$editorPath=Join-Path $PSScriptRoot 'EditorCompile.csproj'
[IO.File]::WriteAllText($editorPath,$editor,[Text.UTF8Encoding]::new($false))
& dotnet build $editorPath --nologo --verbosity quiet
if($LASTEXITCODE -ne 0){throw 'Modified editor tool compilation failed'}
$player=[regex]::Replace($text,'UNITY_EDITOR(?:_[A-Z0-9]+)?;?','')
$player=[regex]::Replace($player,'(?m)^<Reference Include="UnityEditor[^\n]*\n?','')
$playerPath=Join-Path $PSScriptRoot 'PlayerCompile.csproj'
[IO.File]::WriteAllText($playerPath,$player,[Text.UTF8Encoding]::new($false))
& dotnet build $playerPath --nologo --verbosity quiet
if($LASTEXITCODE -ne 0){throw 'Player-define compilation failed'}
