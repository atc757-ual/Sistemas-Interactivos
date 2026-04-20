Add-Type -AssemblyName System.Drawing
$files = Get-ChildItem 'C:\Users\Alex\.gemini\antigravity\brain\7f1d017b-5369-4d95-b56a-27fd432ee2e4\transp_*.png'
foreach ($f in $files) {
    if ($f.Name -notmatch "transparent") {
        $bmp = New-Object System.Drawing.Bitmap($f.FullName)
        $c = $bmp.GetPixel(0,0)
        $bmp.MakeTransparent($c)
        $newName = "Assets\Images\MenuIcons\" + $f.Name.Replace('.png', '_transparent.png')
        $bmp.Save((Join-Path (Get-Location) $newName), [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        Write-Host "Procesado y guardado en: $newName"
    }
}
