using System.IO;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using Logger = BepInEx.Logging.Logger;

namespace Archipelago.UI;

class ArchipelagoIcon
{
    static readonly ManualLogSource logger = Logger.CreateLogSource("ArchipelagoIcon");
    private static Sprite? _cachedIcon;
    public static Sprite? LoadIcon()
    {
        if (_cachedIcon != null)
            return _cachedIcon;

        string path = Path.Combine(Paths.PluginPath, "./assets/archipelago_logo.png");

        if (!File.Exists(path))
        {
            logger.LogWarning($"Logo not found at {path}");
            return null;
        }

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(bytes); // load PNG into Texture2D

        // Create a sprite from the texture
        _cachedIcon = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f) // pivot center
        );

        return _cachedIcon;
    }
}
