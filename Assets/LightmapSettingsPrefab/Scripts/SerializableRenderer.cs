using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerializableRenderer
{
    public Renderer renderer;
    public int lightmapIndex;
    public int realtimeLightmapIndex;
    public Vector4 lightmapScaleOffset;
    public Vector4 realtimeLightmapScaleOffset;
}