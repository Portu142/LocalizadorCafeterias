Add-Type -AssemblyName System.Drawing

$width = 1200
$height = 630
$bitmap = New-Object System.Drawing.Bitmap($width, $height)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

$dark = [System.Drawing.ColorTranslator]::FromHtml('#241811')
$mid = [System.Drawing.ColorTranslator]::FromHtml('#3f2d23')
$cream = [System.Drawing.ColorTranslator]::FromHtml('#faf5ee')
$caramel = [System.Drawing.ColorTranslator]::FromHtml('#d99a5b')
$muted = [System.Drawing.ColorTranslator]::FromHtml('#d4b8a0')

$rect = New-Object System.Drawing.Rectangle(0, 0, $width, $height)
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $mid, $dark, 25.0)
$graphics.FillRectangle($brush, $rect)

$glow = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(26, 217, 154, 91))
$graphics.FillEllipse($glow, 860, -170, 520, 520)
$graphics.FillEllipse($glow, -180, 430, 420, 420)

$eAcute = [char]0x00E9
$uAcute = [char]0x00FA
$enye = [char]0x00F1
$middot = [char]0x00B7
$title = "Localizador de Caf${eAcute}s"
$subtitle = "Caf${eAcute} de especialidad ${middot} Madrid"
$footer = ".NET 8  ${middot}  PostgreSQL/PostGIS  ${middot}  React"

$titleFont = New-Object System.Drawing.Font('Georgia', 78, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$subtitleFont = New-Object System.Drawing.Font('Segoe UI', 34, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$footerFont = New-Object System.Drawing.Font('Segoe UI', 24, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$badgeFont = New-Object System.Drawing.Font('Segoe UI', 20, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)

$creamBrush = New-Object System.Drawing.SolidBrush($cream)
$caramelBrush = New-Object System.Drawing.SolidBrush($caramel)
$mutedBrush = New-Object System.Drawing.SolidBrush($muted)

# Accent bar
$graphics.FillRectangle($caramelBrush, 96, 150, 92, 8)

# Badge
$badgeRect = New-Object System.Drawing.Rectangle(96, 86, 340, 44)
$badgeBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(38, 217, 154, 91))
$graphics.FillRectangle($badgeBrush, $badgeRect)
$graphics.DrawString("PROYECTO DE PORTAFOLIO", $badgeFont, $caramelBrush, 112, 96)

$graphics.DrawString($title, $titleFont, $creamBrush, 88, 188)
$graphics.DrawString($subtitle, $subtitleFont, $caramelBrush, 96, 320)

$linePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(70, 212, 184, 160), 2)
$graphics.DrawLine($linePen, 96, 402, 1104, 402)

$bodyFont = New-Object System.Drawing.Font('Segoe UI', 27, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$graphics.DrawString("Mapa interactivo, horarios, men${uAcute}s y rese${enye}as reales.", $bodyFont, $mutedBrush, 96, 438)

$graphics.DrawString($footer, $footerFont, $mutedBrush, 96, 528)

# Simple coffee cup mark
$cupBrush = New-Object System.Drawing.SolidBrush($cream)
$graphics.FillRectangle($cupBrush, 980, 452, 110, 84)
$graphics.FillEllipse($cupBrush, 960, 440, 150, 26)
$graphics.FillEllipse($cupBrush, 960, 522, 150, 26)
$handlePen = New-Object System.Drawing.Pen($cream, 16)
$graphics.DrawArc($handlePen, 1078, 468, 66, 52, -90, 180)

$steamPen = New-Object System.Drawing.Pen($caramel, 7)
$steamPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$steamPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$graphics.DrawBezier($steamPen, 1002, 420, 982, 392, 1022, 372, 1002, 344)
$graphics.DrawBezier($steamPen, 1042, 420, 1022, 392, 1062, 372, 1042, 344)
$graphics.DrawBezier($steamPen, 1082, 420, 1062, 392, 1102, 372, 1082, 344)

$output = Join-Path $PSScriptRoot '..\public\og-image.png'
$bitmap.Save($output, [System.Drawing.Imaging.ImageFormat]::Png)

$graphics.Dispose()
$bitmap.Dispose()

Write-Output "OG image saved to $output"
