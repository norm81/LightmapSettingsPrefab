using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerializableLightmapData
{
    /// <summary>
    /// Lightmap storing color of incoming light.
    /// </summary>
    public Texture2D lightmapColor;

    /// <summary>
    /// Lightmap storing dominant direction of incoming light.
    /// </summary>
    public Texture2D lightmapDir;

    /// <summary>
    /// Texture storing occlusion mask per light (ShadowMask, up to four lights).
    /// </summary>
    public Texture2D shadowMask;
}