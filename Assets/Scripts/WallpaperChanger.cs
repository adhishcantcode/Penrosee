
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class WallpaperChanger : MonoBehaviour
{
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool SystemParametersInfo(
        uint uAction,
        uint uParam,
        string lpvParam,
        uint fuWinIni
    );
#endif

    private const uint SPI_SETDESKWALLPAPER = 0x0014;
    private const uint SPIF_UPDATEINIFILE = 0x01;
    private const uint SPIF_SENDWININICHANGE = 0x02;

    public void SetWallpaper()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

        string imagePath = Path.Combine(
            Application.streamingAssetsPath,
            "imgg.png"
        );

        if (!File.Exists(imagePath))
        {
            Debug.LogError("Wallpaper image not found: " + imagePath);
            return;
        }

        bool success = SystemParametersInfo(
            SPI_SETDESKWALLPAPER,
            0,
            imagePath,
            SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE
        );

        if (success)
        {
            Debug.Log("Wallpaper changed successfully.");
        }
        else
        {
            Debug.LogError(
                "Wallpaper change failed. Windows error: " +
                Marshal.GetLastWin32Error()
            );
        }

#else
        Debug.LogWarning(
            "Wallpaper changing is supported only in a Windows build."
        );
#endif
    }
}
