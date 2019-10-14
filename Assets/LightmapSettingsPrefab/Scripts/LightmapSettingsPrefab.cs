using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// LightmapSettingsのプレハブ保存データ。
/// </summary>
public class LightmapSettingsPrefab : MonoBehaviour
{
#region LightmapSettings
    public SerializableLightmapData[] lightmaps;
    public LightmapsMode lightmapsMode;
    public LightProbes lightProbes;
    public Cubemap[] reflectionProbes;
#endregion

    public SerializableRenderer[] renderers;
    MaterialPropertyBlock materialPropertyBlock;

    [HideInInspector]
    public bool baked;
    [HideInInspector]
    public string unityVersion;
    bool initialized;

    static readonly int unity_Lightmap = Shader.PropertyToID("unity_Lightmap");
    static readonly int unity_LightmapInd = Shader.PropertyToID("unity_LightmapInd");
    static readonly int unity_LightmapST = Shader.PropertyToID("unity_LightmapST");
    static readonly int unity_SpecCube0 = Shader.PropertyToID("unity_SpecCube0");
    static readonly int unity_SpecCube1 = Shader.PropertyToID("unity_SpecCube1");

    void OnEnable()
    {
        if (!baked)
        {
            return;
        }
        SetupLightmap();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!baked || initialized)
        {
            return;
        }
        SetupLightmap();
    }
#endif

    public void SetupLightmap()
    {
        LightmapData[] lightmaps = null;
        if (this.lightmaps != null)
        {
            lightmaps = new LightmapData[this.lightmaps.Length];
            for (var i = 0; i < this.lightmaps.Length; i++)
            {
                lightmaps[i] = new LightmapData();
                lightmaps[i].lightmapColor = lightmaps[i].lightmapColor;
                lightmaps[i].lightmapDir = lightmaps[i].lightmapDir;
                lightmaps[i].shadowMask = lightmaps[i].shadowMask;
            }
        }
        LightmapSettings.lightmaps = lightmaps;
        LightmapSettings.lightmapsMode = lightmapsMode;
        LightmapSettings.lightProbes = lightProbes;
 
        if (this.renderers != null)
        {
            if (materialPropertyBlock == null)
            {
                materialPropertyBlock = new MaterialPropertyBlock(); 
            }
            for (var i = 0; i < this.renderers.Length; i++)
            {
                var renderer = this.renderers[i].renderer;
                renderer.lightmapIndex = this.renderers[i].lightmapIndex;
                renderer.realtimeLightmapIndex = this.renderers[i].realtimeLightmapIndex;
                renderer.lightmapScaleOffset = this.renderers[i].lightmapScaleOffset;
                renderer.realtimeLightmapScaleOffset = this.renderers[i].realtimeLightmapScaleOffset;

                renderer.GetPropertyBlock(materialPropertyBlock);
                if (renderer.lightmapIndex >= 0 && renderer.lightmapIndex < this.lightmaps.Length)
                {
                    if (this.lightmaps[renderer.lightmapIndex].lightmapColor != null)
                    {
                        materialPropertyBlock.SetTexture(unity_Lightmap, this.lightmaps[renderer.lightmapIndex].lightmapColor);
                    }
                    if (this.lightmaps[renderer.lightmapIndex].lightmapDir != null)
                    {
                        materialPropertyBlock.SetTexture(unity_LightmapInd, this.lightmaps[renderer.lightmapIndex].lightmapDir);
                    }
                }
                materialPropertyBlock.SetVector(unity_LightmapST, renderer.lightmapScaleOffset);
                if (renderer.lightmapIndex >= 0 && renderer.lightmapIndex < reflectionProbes.Length)
                {
                    materialPropertyBlock.SetTexture(unity_SpecCube0, reflectionProbes[renderer.lightmapIndex]);
                    materialPropertyBlock.SetTexture(unity_SpecCube1, reflectionProbes[renderer.lightmapIndex]);
                }
                renderer.SetPropertyBlock(materialPropertyBlock);
            }
        }
        initialized = true;
    }
}
