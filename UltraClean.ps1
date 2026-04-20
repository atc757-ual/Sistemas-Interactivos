$code = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;

public class UltraCleaner {
    public static void Clean(string inFile, string outFile) {
        Bitmap bmp = new Bitmap(inFile);
        Bitmap newBmp = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format32bppArgb);
        
        using (Graphics g = Graphics.FromImage(newBmp)) {
            g.DrawImage(bmp, 0, 0);
        }

        Rectangle rect = new Rectangle(0, 0, newBmp.Width, newBmp.Height);
        BitmapData bmpData = newBmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        
        IntPtr ptr = bmpData.Scan0;
        int bytes = Math.Abs(bmpData.Stride) * newBmp.Height;
        byte[] rgbValues = new byte[bytes];
        Marshal.Copy(ptr, rgbValues, 0, bytes);
        
        byte bgB = rgbValues[0];
        byte bgG = rgbValues[1];
        byte bgR = rgbValues[2];
        
        for (int i = 0; i < rgbValues.Length; i += 4) {
            int b = rgbValues[i];
            int g = rgbValues[i + 1];
            int r = rgbValues[i + 2];
            
            // Chroma Key agresivo
            bool isGreen = (g > 150 && r < 150 && b < 150) || (g > r + 30 && g > b + 30);
            
            if (isGreen) {
                rgbValues[i] = 0;
                rgbValues[i+1] = 0;
                rgbValues[i+2] = 0;
                rgbValues[i+3] = 0; // Transparente puro
            }
        }
        
        Marshal.Copy(rgbValues, 0, ptr, bytes);
        newBmp.UnlockBits(bmpData);
        
        bmp.Dispose();
        
        newBmp.Save(outFile, ImageFormat.Png);
        newBmp.Dispose();
    }
}
"@

Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing

$files = Get-ChildItem 'C:\Users\Alex\.gemini\antigravity\brain\7f1d017b-5369-4d95-b56a-27fd432ee2e4\transp_*.png' 
foreach ($f in $files) {
    if ($f.Name -notmatch "final") {
        $newName = "Assets\Images\MenuIcons\" + $f.Name.Replace('.png', '_final.png')
        [UltraCleaner]::Clean($f.FullName, (Join-Path (Get-Location) $newName))
        Write-Host "Generador Transparente Ultra: $newName"
    }
}
