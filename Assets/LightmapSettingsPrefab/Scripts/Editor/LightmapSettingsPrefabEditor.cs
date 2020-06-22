using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// LightmapSettingsPrefab インスペクタ拡張
/// </summary>
[CanEditMultipleObjects]
[CustomEditor(typeof(LightmapSettingsPrefab))]
public class LightmapSettingsPrefabEditor : Editor
{
    /// <summary>
    /// インスペクタ描画
    /// </summary>
    /// <param name="go"></param>
    public override void OnInspectorGUI()
    {
        var instance = (LightmapSettingsPrefab)target;

        EditorGUI.BeginDisabledGroup(true);
        if (instance.baked)
        {
            base.OnInspectorGUI();
        }
        else
        {
            serializedObject.Update();
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), typeof(MonoScript), false);
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginChangeCheck();

        DrawBakeButton();

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
    }

    /// <summary>
    /// ベイクボタン描画。
    /// </summary>
    void DrawBakeButton()
    {
        var instance = (LightmapSettingsPrefab)target;
        var go = instance.gameObject;
        var prefab = PrefabUtility.GetPrefabObject(go);
        var path = AssetDatabase.GetAssetPath(prefab);

        if (prefab != null && !string.IsNullOrEmpty(path))
        {
            if (GUILayout.Button("Select an object in the hierarchy and bake.", EditorStyles.helpBox))
            {
                Selection.activeObject = PrefabUtility.GetPrefabParent(go);
                EditorGUIUtility.PingObject(Selection.activeObject);
            }
            return;
        }

        var clearBeginDisabledGroup = false;
        var bakeBeginDisabledGroup = false;

        if (Lightmapping.isRunning)
        {
            clearBeginDisabledGroup = true;
            bakeBeginDisabledGroup = true;
        }
        if (!instance.baked)
        {
            clearBeginDisabledGroup = true;
        }
        if (!string.IsNullOrEmpty(path))
        {
            bakeBeginDisabledGroup = true;
        }

        GUILayout.BeginHorizontal();
        GUILayout.Space(EditorGUIUtility.labelWidth);
        EditorGUI.BeginDisabledGroup(clearBeginDisabledGroup);
        if (GUILayout.Button("Clear"))
        {
            DoClear();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUI.BeginDisabledGroup(bakeBeginDisabledGroup);
        if (GUILayout.Button("Bake"))
        {
            DoBake();
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// クリアする
    /// </summary>
    void DoClear()
    {
        Lightmapping.ClearLightingDataAsset();
        Lightmapping.Clear();

        var instance = (LightmapSettingsPrefab)target;
        var go = instance.gameObject;

        instance.baked = default(bool);
        instance.unityVersion = default(string);
        instance.lightmaps = default(SerializableLightmapData[]);
        instance.lightmapsMode = default(LightmapsMode);
        instance.lightProbes = default(LightProbes);
        instance.reflectionProbes = default(Cubemap[]);
        instance.renderers = default(SerializableRenderer[]);
        instance.SetupLightmap();

        DoApply(go);
    }

    /// <summary>
    /// Bakeする
    /// </summary>
    void DoBake()
    {
        Lightmapping.completed = OnCompletedFunction;
        Lightmapping.BakeAsync();
    }

    /// <summary>
    /// Bake完了コールバック
    /// </summary>
    void OnCompletedFunction()
    {
        Lightmapping.completed = null;

        var instance = (LightmapSettingsPrefab)target;
        var go = instance.gameObject;

        var scene = SceneManager.GetActiveScene();
        var directory = Regex.Replace(scene.path, @".unity", @"/", RegexOptions.IgnoreCase);

        instance.baked = true;
        instance.unityVersion = Application.unityVersion;
        instance.lightmaps = CreateLightmaps(directory);
        instance.lightmapsMode = LightmapSettings.lightmapsMode;
        instance.lightProbes = CreateLightProbes(directory);
        instance.reflectionProbes = CreateReflectionProbes(directory);

        var renderers = instance.GetComponentsInChildren(typeof(Renderer), true);
        instance.renderers = new SerializableRenderer[renderers.Length];
        for (var i = 0; i < renderers.Length; i++)
        {
            instance.renderers[i] = new SerializableRenderer();
            var renderer = renderers[i] as Renderer;
            instance.renderers[i].renderer = renderer;
            instance.renderers[i].lightmapIndex = renderer.lightmapIndex;
            instance.renderers[i].realtimeLightmapIndex = renderer.realtimeLightmapIndex;
            instance.renderers[i].lightmapScaleOffset = renderer.lightmapScaleOffset;
            instance.renderers[i].realtimeLightmapScaleOffset = renderer.realtimeLightmapScaleOffset;
        }

        instance.SetupLightmap();

        DoApply(go);
    }

    /// <summary>
    /// LightmapData作成
    /// </summary>
    /// <param name="directory"></param>
    /// <returns>LightmapData</returns>
    SerializableLightmapData[] CreateLightmaps(string directory)
    {
        var directiryInfo = new DirectoryInfo(directory);

        var lightmapColorInfos = directiryInfo.GetFiles("Lightmap-*_comp_light.exr");
        var lightmapDirInfos = directiryInfo.GetFiles("Lightmap-*_comp_dir.png");
        var shadowMaskInfos = directiryInfo.GetFiles("Lightmap-*_comp_shadowmask.png");

        System.Array.Sort(lightmapColorInfos, LightmapComparer);
        System.Array.Sort(lightmapDirInfos, LightmapComparer);
        System.Array.Sort(shadowMaskInfos, LightmapComparer);

        var lightmapsLength = Mathf.Max(Mathf.Max(lightmapColorInfos.Length, lightmapDirInfos.Length), shadowMaskInfos.Length);
        var lightmaps = new SerializableLightmapData[lightmapsLength];
        for (var i = 0; i < lightmapsLength; i++)
        {
            lightmaps[i] = new SerializableLightmapData();
            if (i < lightmapColorInfos.Length)
            {
                lightmaps[i].lightmapColor = AssetDatabase.LoadAssetAtPath<Texture2D>(directory + lightmapColorInfos[i].Name);
            }
            if (i < lightmapDirInfos.Length)
            {
                lightmaps[i].lightmapDir = AssetDatabase.LoadAssetAtPath<Texture2D>(directory + lightmapDirInfos[i].Name);
            }
            if (i < shadowMaskInfos.Length)
            {
                lightmaps[i].shadowMask = AssetDatabase.LoadAssetAtPath<Texture2D>(directory + shadowMaskInfos[i].Name);
            }
        }
        return lightmaps;
    }

    /// <summary>
    /// LightProbs作成
    /// </summary>
    /// <param name="directory"></param>
    /// <returns>LightProbs</returns>
    LightProbes CreateLightProbes(string directory)
    {
        // GUIDを保持するため別の場所にコピー。
        var instance = GameObject.Instantiate(LightmapSettings.lightProbes);
        AssetDatabase.CreateAsset(instance, "Assets/LightProbes.asset");
        AssetDatabase.Refresh();

        File.Copy("Assets/LightProbes.asset", directory + "LightProbes.asset", true);
        AssetDatabase.DeleteAsset("Assets/LightProbes.asset");
        AssetDatabase.ImportAsset(directory + "LightProbes.asset");
        AssetDatabase.Refresh();
        return AssetDatabase.LoadAssetAtPath<LightProbes>(directory + "LightProbes.asset");
    }

    /// <summary>
    /// ReflectionProbe作成
    /// </summary>
    /// <param name="directory"></param>
    /// <returns>ReflectionProbe</returns>
    Cubemap[] CreateReflectionProbes(string directory)
    {
        var directiryInfo = new DirectoryInfo(directory);

        var reflectionProbeInfos = directiryInfo.GetFiles("ReflectionProbe-*.exr");

        System.Array.Sort(reflectionProbeInfos, ReflectionProbeComparer);

        var reflectionProbesLength = reflectionProbeInfos.Length;
        var reflectionProbes = new Cubemap[reflectionProbesLength];
        for (var i = 0; i < reflectionProbesLength; i++)
        {
            reflectionProbes[i] = AssetDatabase.LoadAssetAtPath<Cubemap>(directory + reflectionProbeInfos[i].Name);
        }
        return reflectionProbes;
    }

    /// <summary>
    /// Applyする。
    /// </summary>
    /// <param name="go"></param>
    void DoApply(GameObject go)
    {
        var root = PrefabUtility.FindPrefabRoot(go);
        var parent = PrefabUtility.GetPrefabParent(root);
        PrefabUtility.ReplacePrefab(root, parent, ReplacePrefabOptions.ConnectToPrefab);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Lightmap命名規則に基づいた比較関数
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    static int LightmapComparer(FileInfo a, FileInfo b)
    {
        // Lightmap-(*)_comp_*.*
        var an = int.Parse(a.Name.Replace("Lightmap-", string.Empty).Split('_')[0]);
        var bn = int.Parse(b.Name.Replace("Lightmap-", string.Empty).Split('_')[0]);
        return an - bn;
    }

    /// <summary>
    /// ReflectionProbe命名規則に基づいた比較関数
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    static int ReflectionProbeComparer(FileInfo a, FileInfo b)
    {
        // ReflectionProbe-(*).exr
        var an = int.Parse(a.Name.Replace("ReflectionProbe-", string.Empty).Split('.')[0]);
        var bn = int.Parse(b.Name.Replace("ReflectionProbe-", string.Empty).Split('.')[0]);
        return an - bn;
    }
}