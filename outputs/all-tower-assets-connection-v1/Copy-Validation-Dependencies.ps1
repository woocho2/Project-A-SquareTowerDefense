$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskPluginDir=Join-Path $PSScriptRoot 'UnityValidation/Assets/Plugins'
$taskSources=@{}
foreach($taskDir in @('Library/ScriptAssemblies','Assets/Plugins','Library/PackageCache')){
    $taskPath=Join-Path $taskRoot $taskDir
    if(!(Test-Path -LiteralPath $taskPath)){continue}
    foreach($taskFile in (Get-ChildItem -LiteralPath $taskPath -File -Recurse -Filter '*.dll')){
        if($taskFile.Name -like '*.ref.dll' -or $taskFile.Name -in @('Assembly-CSharp.dll','Assembly-CSharp-Editor.dll')){continue}
        if(!$taskSources.ContainsKey($taskFile.BaseName)){$taskSources[$taskFile.BaseName]=$taskFile.FullName}
    }
}
$taskBuiltin=@{}
foreach($taskFile in (Get-ChildItem -LiteralPath 'C:/Program Files/Unity/Hub/Editor/6000.3.9f1/Editor/Data/Managed' -File -Recurse -Filter '*.dll')){$taskBuiltin[$taskFile.BaseName]=$true}
$taskQueue=New-Object 'System.Collections.Generic.Queue[string]'
foreach($taskFile in (Get-ChildItem -LiteralPath $taskPluginDir -Filter '*.dll')){$taskQueue.Enqueue($taskFile.BaseName)}
$taskVisited=New-Object 'System.Collections.Generic.HashSet[string]'
while($taskQueue.Count){
    $taskName=$taskQueue.Dequeue();if(!$taskVisited.Add($taskName)){continue}
    if(!$taskSources.ContainsKey($taskName)){throw ('Missing validation assembly source: '+$taskName)}
    $taskSource=$taskSources[$taskName]
    Copy-Item -LiteralPath $taskSource -Destination (Join-Path $taskPluginDir ($taskName+'.dll')) -Force
    $taskAssembly=[Reflection.Assembly]::ReflectionOnlyLoadFrom($taskSource)
    foreach($taskReference in $taskAssembly.GetReferencedAssemblies()){
        $taskRefName=$taskReference.Name
        if($taskBuiltin.ContainsKey($taskRefName) -or $taskRefName -match '^(mscorlib|netstandard|System(?:\.|$)|Microsoft\.)'){continue}
        if($taskSources.ContainsKey($taskRefName)){$taskQueue.Enqueue($taskRefName)}else{throw ('Unresolved validation reference: '+$taskRefName+' from '+$taskName)}
    }
}
Write-Output ('Copied native validation assembly dependency closure: '+$taskVisited.Count+' libraries.')
