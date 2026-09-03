Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = 'Stop'
$outputDirectory = 'Assets/4. DotAsset/2. Tower/Parts/Emblem'
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

function P([int]$x, [int]$y) { [System.Drawing.Point]::new($x, $y) }
function Draw-Polygon($graphics, $brush, $points) { $graphics.FillPolygon($brush, [System.Drawing.Point[]]$points) }
function Draw-Line($graphics, $pen, [int]$x1, [int]$y1, [int]$x2, [int]$y2) { $graphics.DrawLine($pen, $x1, $y1, $x2, $y2) }
function Draw-RunicHalo($graphics, $black) {
    $thin = [System.Drawing.Pen]::new([System.Drawing.Color]::Black, 6)
    $thin.StartCap = [System.Drawing.Drawing2D.LineCap]::Square
    $thin.EndCap = [System.Drawing.Drawing2D.LineCap]::Square
    try {
        $graphics.DrawArc($thin, 24, 13, 152, 164, 198, 44)
        $graphics.DrawArc($thin, 24, 13, 152, 164, 288, 44)
        $graphics.DrawArc($thin, 24, 13, 152, 164, 18, 44)
        $graphics.DrawArc($thin, 24, 13, 152, 164, 108, 44)
        foreach ($mark in @(@(100,16), @(171,95), @(100,174), @(29,95))) {
            $x=$mark[0]; $y=$mark[1]
            Draw-Polygon $graphics $black @((P $x ($y-7)), (P ($x+7) $y), (P $x ($y+7)), (P ($x-7) $y))
        }
    }
    finally { $thin.Dispose() }
}

$emblems = @('Sword', 'Bow', 'Shield', 'Spear', 'Axe', 'Hammer', 'Fire', 'Ice', 'Lightning', 'Wind', 'Earth', 'Light', 'Dark')

foreach ($name in $emblems) {
    $bitmap = [System.Drawing.Bitmap]::new(200, 190, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $black = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::Black)
    $pen = [System.Drawing.Pen]::new([System.Drawing.Color]::Black, 14)
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Square
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Square
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Miter

    try {
        switch ($name) {
            'Sword' {
                Draw-Polygon $graphics $black @((P 28 31), (P 52 18), (P 144 110), (P 141 125), (P 128 138), (P 113 141), (P 21 49))
                Draw-Polygon $graphics $black @((P 89 115), (P 103 101), (P 151 149), (P 137 163))
                Draw-Polygon $graphics $black @((P 124 150), (P 142 132), (P 168 158), (P 150 176))
                Draw-Polygon $graphics $black @((P 77 133), (P 93 117), (P 129 153), (P 113 169))
                $cut = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::Transparent)
                Draw-Polygon $graphics $cut @((P 42 35), (P 51 31), (P 126 106), (P 121 115))
                $cut.Dispose()
            }
            'Bow' {
                $graphics.DrawArc($pen, 23, 18, 135, 154, 95, 170)
                Draw-Line $graphics $pen 133 30 133 160
                Draw-Line $graphics $pen 48 95 155 95
                Draw-Polygon $graphics $black @((P 164 95), (P 142 81), (P 142 109))
                Draw-Polygon $graphics $black @((P 53 95), (P 70 84), (P 70 106))
            }
            'Shield' {
                Draw-Polygon $graphics $black @((P 100 20), (P 158 43), (P 150 116), (P 100 166), (P 50 116), (P 42 43))
                $inner = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::Transparent)
                $graphics.FillPolygon($inner, [System.Drawing.Point[]]@((P 100 43), (P 135 57), (P 130 105), (P 100 136), (P 70 105), (P 65 57)))
                $inner.Dispose()
            }
            'Spear' {
                Draw-Line $graphics $pen 41 151 148 44
                Draw-Polygon $graphics $black @((P 155 18), (P 169 51), (P 149 80), (P 127 58))
                Draw-Polygon $graphics $black @((P 76 98), (P 97 119), (P 82 134), (P 61 113))
                Draw-Polygon $graphics $black @((P 30 151), (P 55 142), (P 49 167))
            }
            'Axe' {
                Draw-Line $graphics $pen 111 39 75 169
                Draw-Polygon $graphics $black @((P 96 34), (P 50 40), (P 27 66), (P 33 104), (P 60 124), (P 96 114), (P 118 86), (P 117 49))
                $cut = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::Transparent)
                Draw-Polygon $graphics $cut @((P 88 54), (P 61 59), (P 47 76), (P 51 94), (P 66 103), (P 84 97), (P 96 79))
                $cut.Dispose()
            }
            'Hammer' {
                Draw-Line $graphics $pen 100 78 100 167
                Draw-Polygon $graphics $black @((P 39 40), (P 154 40), (P 166 52), (P 166 77), (P 154 89), (P 39 89), (P 27 77), (P 27 52))
                $cut = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::Transparent)
                Draw-Polygon $graphics $cut @((P 48 52), (P 139 52), (P 145 58), (P 139 65), (P 48 65))
                $cut.Dispose()
            }
            'Fire' {
                Draw-Polygon $graphics $black @((P 103 16), (P 132 57), (P 127 76), (P 151 101), (P 140 151), (P 101 174), (P 59 150), (P 50 110), (P 74 78), (P 72 46))
                $cut = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::Transparent)
                Draw-Polygon $graphics $cut @((P 99 68), (P 119 101), (P 111 117), (P 122 137), (P 99 155), (P 79 134), (P 86 108))
                $cut.Dispose()
            }
            'Ice' {
                Draw-Line $graphics $pen 100 20 100 170
                Draw-Line $graphics $pen 38 55 162 135
                Draw-Line $graphics $pen 162 55 38 135
                Draw-Line $graphics $pen 100 42 79 63
                Draw-Line $graphics $pen 100 42 121 63
                Draw-Line $graphics $pen 100 148 79 127
                Draw-Line $graphics $pen 100 148 121 127
                Draw-Line $graphics $pen 59 69 61 96
                Draw-Line $graphics $pen 59 69 83 78
                Draw-Line $graphics $pen 141 121 139 94
                Draw-Line $graphics $pen 141 121 117 112
            }
            'Lightning' {
                Draw-Polygon $graphics $black @((P 111 16), (P 55 105), (P 92 105), (P 76 174), (P 147 77), (P 109 77))
            }
            'Wind' {
                $graphics.DrawArc($pen, 35, 41, 130, 55, 185, 170)
                $graphics.DrawArc($pen, 56, 76, 112, 48, 190, 165)
                $graphics.DrawArc($pen, 30, 115, 132, 46, 185, 155)
            }
            'Earth' {
                Draw-Polygon $graphics $black @((P 25 137), (P 71 76), (P 99 105), (P 131 43), (P 175 137))
                Draw-Polygon $graphics $black @((P 22 140), (P 178 140), (P 178 167), (P 22 167))
                $cut = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::Transparent)
                Draw-Polygon $graphics $cut @((P 123 61), (P 131 53), (P 140 68), (P 132 83))
                Draw-Polygon $graphics $cut @((P 72 95), (P 79 86), (P 90 102), (P 82 113))
                Draw-Polygon $graphics $cut @((P 38 148), (P 162 148), (P 162 154), (P 38 154))
                $cut.Dispose()
            }
            'Light' {
                $graphics.FillEllipse($black, 66, 56, 68, 68)
                foreach ($ray in @(@(100,18,100,42), @(100,148,100,172), @(28,90,52,90), @(148,90,172,90), @(48,38,65,55), @(135,125,152,142), @(152,38,135,55), @(65,125,48,142))) {
                    Draw-Line $graphics $pen $ray[0] $ray[1] $ray[2] $ray[3]
                }
            }
            'Dark' {
                Draw-Polygon $graphics $black @((P 23 95), (P 62 52), (P 100 39), (P 138 52), (P 177 95), (P 138 138), (P 100 151), (P 62 138))
                $cut = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::Transparent)
                $graphics.FillEllipse($cut, 55, 70, 90, 50)
                $cut.Dispose()
                $graphics.FillEllipse($black, 91, 72, 18, 46)
                Draw-Polygon $graphics $black @((P 35 62), (P 48 47), (P 55 68))
                Draw-Polygon $graphics $black @((P 145 68), (P 152 47), (P 165 62))
            }
        }

        $bitmap.Save((Join-Path $outputDirectory ($name + '_Emblem.png')), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $pen.Dispose()
        $black.Dispose()
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}
