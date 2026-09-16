$path = 'd:\MyGitHub\ProjectA\Project-A-SquareTowerDefense\Assets\4. DotAsset\Ho6XZNkf03aNk_9XYKj4UlJQ0jW58MPkytlqUwtgkNsXFymixntKq1yzP0CaNS3lSPRhWbsNEGroQ1TmNhyPgQ.mp4'
$shell = New-Object -ComObject Shell.Application
$folder = $shell.Namespace((Split-Path $path))
$file = $folder.ParseName((Split-Path $path -Leaf))

for ($i = 0; $i -lt 320; $i++) {
    $name = $folder.GetDetailsOf($null, $i)
    $val = $folder.GetDetailsOf($file, $i)
    if ($val -and ($name -match '길이|프레임|너비|높이|Width|Height|Frame|Length|Duration')) {
        Write-Host "$name ($i) = $val"
    }
}
