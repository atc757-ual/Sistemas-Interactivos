$code = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;

public class ImageCleaner {
    public static void Clean(string inFile, string outFile) {
        Bitmap bmp = new Bitmap(inFile);
        
        Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        
        IntPtr ptr = bmpData.Scan0;
        int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
        byte[] rgbValues = new byte[bytes];
        Marshal.Copy(ptr, rgbValues, 0, bytes);
        
        // Background color based on pixel (0,0) -> BGRA
        byte bgB = rgbValues[0];
        byte bgG = rgbValues[1];
        byte bgR = rgbValues[2];
        
        for (int i = 0; i < rgbValues.Length; i += 4) {
            byte b = rgbValues[i];
            byte g = rgbValues[i + 1];
            byte r = rgbValues[i + 2];
            
            // Evaluamos la distancia de color real al verde cromático del fondo para eliminar el halo
            double distToBg = Math.Sqrt(Math.Pow(b - bgB, 2) + Math.Pow(g - bgG, 2) + Math.Pow(r - bgR, 2));
            
            // Chroma key heurístico (cualquier verde brillante puro)
            bool isChromaGreen = (g > 150 && r < 120 && b < 120);
            
            if (distToBg < 110 || isChromaGreen) {
                 rgbValues[i + 3] = 0; // Transparente 100%
            }
            // Para suavizar el pequeño halo que queda, si es medio verde le bajamos la opacidad
            else if (g > r && g > b && distToBg < 160) {
                // Semi transparente y le quitamos saturación verde
                rgbValues[i + 3] = (byte)(rgbValues[i + 3] / 3);
                rgbValues[i + 1] = (byte)((r + b) / 2); // matar el verde extra
            }
        }
        
        Marshal.Copy(rgbValues, 0, ptr, bytes);
        bmp.UnlockBits(bmpData);
        bmp.Save(outFile, ImageFormat.Png);
        bmp.Dispose();
    }
}
"@

Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing

$files = Get-ChildItem 'C:\Users\Alex\.gemini\antigravity\brain\7f1d017b-5369-4d95-b56a-27fd432ee2e4\transp_*.png' 
foreach ($f in $files) {
    if ($f.Name -notmatch "limpio") {
        $newName = "Assets\Images\MenuIcons\" + $f.Name.Replace('.png', '_limpio.png')
        [ImageCleaner]::Clean($f.FullName, (Join-Path (Get-Location) $newName))
        Write-Host "Limpiado halo verde en: $newName"
    }
}
