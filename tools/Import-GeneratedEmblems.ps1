param([string[]]$Only)
Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = 'Stop'
$sourceRoot = 'C:\Users\user\.codex\generated_images\01a0668a-5137-7093-9f78-6dbc3dad59d7'
$targetRoot = 'Assets/4. DotAsset/2. Tower/Parts/Emblem'
$sources = [ordered]@{
    Axe = '03f7535b-1bea-4d58-9c4f-36c7af68fec8'; Sword = '6babfe5b-c527-45e9-b4a8-4659be09070e'
    Shield = '83533fc4-9a52-45dd-a824-7431732f7ec1'; Spear = 'a2afa7ac-db39-4d85-a4aa-ce463052e82f'
    Bow = 'd644f6b6-68c5-4622-82c7-b4b8a905ec3d'; Hammer = 'd516d8bf-7987-4fb9-bbe1-3a939b8dd661'
    Fire = 'db8c337b-bbac-4c78-bca4-e260b17954bb'; Ice = 'e7ea3f54-61ae-4f6d-b9a3-9c3f364cc939'
    Lightning = '076616ec-4b35-4665-ba24-d735fcfe0725'; Wind = '195c329b-668b-4665-8d3e-f50b0074739a'
    Earth = 'fe523aa7-5634-4cab-9b52-9d81bd09e1e4'; Light = 'ff643653-8a7c-4e4a-8d6c-f7a7f973e206'
    Dark = '6b4f145f-4273-4b6b-9b46-606218ed0041'
}

foreach ($item in $sources.GetEnumerator()) {
    if ($Only -and $item.Key -notin $Only) { continue }
    $source = Join-Path $sourceRoot ('exec-' + $item.Value + '.png')
    $input = [System.Drawing.Bitmap]::new($source)
    $minX=$input.Width; $minY=$input.Height; $maxX=-1; $maxY=-1
    for($y=0;$y -lt $input.Height;$y++){ for($x=0;$x -lt $input.Width;$x++){
        $p=$input.GetPixel($x,$y); $ink=255-[int](($p.R+$p.G+$p.B)/3)
        if($p.A -gt 10 -and $ink -gt 55){ if($x -lt $minX){$minX=$x};if($x -gt $maxX){$maxX=$x};if($y -lt $minY){$minY=$y};if($y -gt $maxY){$maxY=$y} }
    }}
    $crop = [System.Drawing.Rectangle]::FromLTRB([Math]::Max(0,$minX-8),[Math]::Max(0,$minY-8),[Math]::Min($input.Width,$maxX+9),[Math]::Min($input.Height,$maxY+9))
    $scale=[Math]::Min(172.0/$crop.Width,162.0/$crop.Height); $w=[int][Math]::Round($crop.Width*$scale);$h=[int][Math]::Round($crop.Height*$scale)
    $output=[System.Drawing.Bitmap]::new(200,190,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g=[System.Drawing.Graphics]::FromImage($output);$g.Clear([System.Drawing.Color]::Transparent)
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.DrawImage($input,[System.Drawing.Rectangle]::new([int]((200-$w)/2),[int]((190-$h)/2),$w,$h),$crop,[System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose();$input.Dispose()
    for($y=0;$y -lt 190;$y++){for($x=0;$x -lt 200;$x++){$p=$output.GetPixel($x,$y);$ink=255-[int](($p.R+$p.G+$p.B)/3);$a=[int]($p.A*[Math]::Min(1.0,[Math]::Max(0.0,($ink-55)/160.0)));if($a -lt 12){$output.SetPixel($x,$y,[System.Drawing.Color]::Transparent)}else{$output.SetPixel($x,$y,[System.Drawing.Color]::FromArgb($a,0,0,0))}}}
    $output.Save((Join-Path $targetRoot ($item.Key+'_Emblem.png')),[System.Drawing.Imaging.ImageFormat]::Png);$output.Dispose()
}
