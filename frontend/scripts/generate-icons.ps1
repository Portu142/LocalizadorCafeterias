# Genera los iconos PNG de la app (apple-touch-icon y PWA) a partir del mismo
# diseño que public/favicon.svg. Uso: powershell -File scripts/generate-icons.ps1
Add-Type -AssemblyName System.Drawing

$publicDir = Join-Path $PSScriptRoot '..\public'

function New-CoffeeIcon([int]$size, [string]$fileName, [bool]$maskable) {
    $bitmap = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $dark = [System.Drawing.ColorTranslator]::FromHtml('#3f2d23')
    $cream = [System.Drawing.ColorTranslator]::FromHtml('#faf5ee')
    $caramel = [System.Drawing.ColorTranslator]::FromHtml('#c47f3b')

    # Escala del lienzo 64x64 del SVG. Los iconos "maskable" ocupan todo el
    # cuadrado y dejan el dibujo dentro de la zona segura (80 %).
    $s = $size / 64.0
    $bg = New-Object System.Drawing.SolidBrush($dark)
    if ($maskable) {
        $g.FillRectangle($bg, 0, 0, $size, $size)
        $g.TranslateTransform($size * 0.1, $size * 0.1)
        $s = $s * 0.8
    } else {
        $r = 16 * $s
        $path = New-Object System.Drawing.Drawing2D.GraphicsPath
        $path.AddArc(0, 0, 2 * $r, 2 * $r, 180, 90)
        $path.AddArc($size - 2 * $r, 0, 2 * $r, 2 * $r, 270, 90)
        $path.AddArc($size - 2 * $r, $size - 2 * $r, 2 * $r, 2 * $r, 0, 90)
        $path.AddArc(0, $size - 2 * $r, 2 * $r, 2 * $r, 90, 90)
        $path.CloseFigure()
        $g.FillPath($bg, $path)
    }

    # Taza: rectángulo de 18,26 a 42,38 con fondo semicircular hasta y=50.
    $cup = New-Object System.Drawing.Drawing2D.GraphicsPath
    $cup.AddLine(18 * $s, 26 * $s, 42 * $s, 26 * $s)
    $cup.AddLine(42 * $s, 26 * $s, 42 * $s, 38 * $s)
    $cup.AddArc(18 * $s, 26 * $s, 24 * $s, 24 * $s, 0, 180)
    $cup.CloseFigure()
    $g.FillPath((New-Object System.Drawing.SolidBrush($cream)), $cup)

    # Asa.
    $pen = New-Object System.Drawing.Pen -ArgumentList $cream, ([single](4 * $s))
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawLine($pen, 42 * $s, 30 * $s, 46 * $s, 30 * $s)
    $g.DrawArc($pen, 40 * $s, 30 * $s, 12 * $s, 12 * $s, 270, 180)
    $g.DrawLine($pen, 46 * $s, 42 * $s, 42 * $s, 42 * $s)

    # Vapor.
    $steam = New-Object System.Drawing.Pen -ArgumentList $caramel, ([single](3 * $s))
    $steam.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $steam.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    foreach ($x in 24, 32, 40) {
        $g.DrawBezier($steam, $x * $s, 18 * $s, $x * $s, 16 * $s, ($x + 2) * $s, 16 * $s, ($x + 2) * $s, 14 * $s)
    }

    $bitmap.Save((Join-Path $publicDir $fileName), [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bitmap.Dispose()
}

New-CoffeeIcon 180 'apple-touch-icon.png' $true
New-CoffeeIcon 192 'icon-192.png' $false
New-CoffeeIcon 512 'icon-512.png' $false
New-CoffeeIcon 512 'icon-maskable-512.png' $true
Write-Host 'Iconos generados en public/'
