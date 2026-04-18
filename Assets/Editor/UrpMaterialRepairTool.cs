using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class UrpMaterialRepairTool
{
    private const string UrpLitShaderName = "Universal Render Pipeline/Lit";
    private const string UrpUnlitShaderName = "Universal Render Pipeline/Unlit";
    private const string UrpParticlesLitShaderName = "Universal Render Pipeline/Particles/Lit";
    private const string UrpParticlesUnlitShaderName = "Universal Render Pipeline/Particles/Unlit";
    private const string ErrorShaderName = "Hidden/InternalErrorShader";

    [MenuItem("Tools/Rendering/URP Material Repair/Scan Project Materials")]
    private static void ScanProjectMaterials()
    {
        var materialPaths = AssetDatabase.FindAssets("t:Material")
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path)
            .ToArray();

        var unsupported = new List<string>();
        foreach (var path in materialPaths)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                continue;
            }

            if (NeedsRepair(material))
            {
                unsupported.Add($"{path} -> {GetShaderName(material)}");
            }
        }

        Debug.Log($"[URP Repair] Scan finished. Unsupported materials: {unsupported.Count}");
        foreach (var entry in unsupported.Take(200))
        {
            Debug.Log($"[URP Repair] {entry}");
        }

        if (unsupported.Count > 200)
        {
            Debug.Log($"[URP Repair] ...and {unsupported.Count - 200} more materials.");
        }
    }

    [MenuItem("Tools/Rendering/URP Material Repair/Repair Selected Folders")]
    private static void RepairSelectedFolders()
    {
        var rootPaths = Selection.assetGUIDs
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(AssetDatabase.IsValidFolder)
            .Distinct()
            .ToArray();

        if (rootPaths.Length == 0)
        {
            EditorUtility.DisplayDialog("URP Material Repair", "请先在 Project 里选中要修复的文件夹。", "OK");
            return;
        }

        RepairMaterials(rootPaths);
    }

    [MenuItem("Tools/Rendering/URP Material Repair/Repair All Project Materials")]
    private static void RepairAllProjectMaterials()
    {
        RepairMaterials(new[] { "Assets" });
    }

    private static void RepairMaterials(string[] rootPaths)
    {
        var materialPaths = CollectMaterialPaths(rootPaths);
        if (materialPaths.Count == 0)
        {
            Debug.Log("[URP Repair] No materials found in the selected scope.");
            return;
        }

        var repairedCount = 0;
        var skippedCount = 0;

        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (var path in materialPaths)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    skippedCount++;
                    continue;
                }

                if (!NeedsRepair(material))
                {
                    skippedCount++;
                    continue;
                }

                if (RepairMaterial(material))
                {
                    repairedCount++;
                    EditorUtility.SetDirty(material);
                }
                else
                {
                    skippedCount++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[URP Repair] Repair complete. Repaired: {repairedCount}, skipped: {skippedCount}");
        EditorUtility.DisplayDialog("URP Material Repair", $"修复完成。\n已修复: {repairedCount}\n跳过: {skippedCount}", "OK");
    }

    private static HashSet<string> CollectMaterialPaths(IEnumerable<string> rootPaths)
    {
        var results = new HashSet<string>();
        foreach (var rootPath in rootPaths)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { rootPath }))
            {
                results.Add(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        return results;
    }

    private static bool RepairMaterial(Material material)
    {
        var shader = PickTargetShader(material);
        if (shader == null)
        {
            Debug.LogWarning($"[URP Repair] Missing target URP shader for material: {material.name}");
            return false;
        }

        var snapshot = MaterialSnapshot.Capture(material);
        material.shader = shader;
        ApplySurfaceSettings(material, snapshot);
        ApplyCommonProperties(material, snapshot);
        ApplyKeywords(material, snapshot);
        return true;
    }

    private static Shader PickTargetShader(Material material)
    {
        if (LooksLikeParticleMaterial(material))
        {
            return Shader.Find(HasTransparentSurface(material) ? UrpParticlesUnlitShaderName : UrpParticlesLitShaderName)
                ?? Shader.Find(UrpParticlesLitShaderName);
        }

        if (LooksLikeUnlitMaterial(material))
        {
            return Shader.Find(UrpUnlitShaderName) ?? Shader.Find(UrpLitShaderName);
        }

        return Shader.Find(UrpLitShaderName);
    }

    private static void ApplyCommonProperties(Material material, MaterialSnapshot snapshot)
    {
        SetTextureIfPresent(material, "_BaseMap", snapshot.BaseMap, snapshot.BaseMapScale, snapshot.BaseMapOffset);
        SetTextureIfPresent(material, "_BumpMap", snapshot.NormalMap, snapshot.NormalMapScale, snapshot.NormalMapOffset);
        SetTextureIfPresent(material, "_EmissionMap", snapshot.EmissionMap, snapshot.EmissionMapScale, snapshot.EmissionMapOffset);
        SetTextureIfPresent(material, "_MetallicGlossMap", snapshot.MetallicGlossMap, snapshot.MetallicMapScale, snapshot.MetallicMapOffset);
        SetTextureIfPresent(material, "_OcclusionMap", snapshot.OcclusionMap, snapshot.OcclusionMapScale, snapshot.OcclusionMapOffset);

        SetColorIfPresent(material, "_BaseColor", snapshot.BaseColor);
        SetColorIfPresent(material, "_EmissionColor", snapshot.EmissionColor);
        SetFloatIfPresent(material, "_Cutoff", snapshot.Cutoff);
        SetFloatIfPresent(material, "_BumpScale", snapshot.NormalStrength);
        SetFloatIfPresent(material, "_Metallic", snapshot.Metallic);
        SetFloatIfPresent(material, "_Smoothness", snapshot.Smoothness);
        SetFloatIfPresent(material, "_OcclusionStrength", snapshot.OcclusionStrength);
        SetFloatIfPresent(material, "_ReceiveShadows", snapshot.ReceiveShadows ? 1f : 0f);
        SetFloatIfPresent(material, "_Cull", snapshot.CullMode);
    }

    private static void ApplySurfaceSettings(Material material, MaterialSnapshot snapshot)
    {
        if (!material.HasProperty("_Surface"))
        {
            return;
        }

        var transparent = snapshot.Transparent;
        material.SetFloat("_Surface", transparent ? 1f : 0f);
        material.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque");

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", transparent ? 0f : 0f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetFloat("_SrcBlend", transparent ? (float)UnityEngine.Rendering.BlendMode.SrcAlpha : (float)UnityEngine.Rendering.BlendMode.One);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat("_DstBlend", transparent ? (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : (float)UnityEngine.Rendering.BlendMode.Zero);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", transparent ? 0f : 1f);
        }

        if (material.HasProperty("_AlphaClip"))
        {
            material.SetFloat("_AlphaClip", snapshot.AlphaClip ? 1f : 0f);
        }

        material.renderQueue = snapshot.AlphaClip
            ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest
            : transparent
                ? (int)UnityEngine.Rendering.RenderQueue.Transparent
                : -1;
    }

    private static void ApplyKeywords(Material material, MaterialSnapshot snapshot)
    {
        if (snapshot.NormalMap != null)
        {
            material.EnableKeyword("_NORMALMAP");
        }
        else
        {
            material.DisableKeyword("_NORMALMAP");
        }

        if (snapshot.EmissionEnabled)
        {
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }

        if (snapshot.AlphaClip)
        {
            material.EnableKeyword("_ALPHATEST_ON");
        }
        else
        {
            material.DisableKeyword("_ALPHATEST_ON");
        }
    }

    private static bool NeedsRepair(Material material)
    {
        var shaderName = GetShaderName(material);
        if (string.IsNullOrEmpty(shaderName))
        {
            return true;
        }

        if (shaderName.StartsWith("Universal Render Pipeline/"))
        {
            return false;
        }

        if (shaderName.StartsWith("TextMeshPro/") || shaderName.StartsWith("GUI/") || shaderName.StartsWith("Sprites/"))
        {
            return false;
        }

        if (shaderName.StartsWith("Skybox/"))
        {
            return false;
        }

        if (shaderName == ErrorShaderName)
        {
            return true;
        }

        if (shaderName.StartsWith("Hidden/"))
        {
            return false;
        }

        return shaderName == "Standard"
            || shaderName == "Standard (Specular setup)"
            || shaderName == "Autodesk Interactive"
            || shaderName.StartsWith("Legacy Shaders/")
            || shaderName.StartsWith("Mobile/")
            || shaderName.StartsWith("Particles/")
            || shaderName.Contains("Built-in_RP")
            || shaderName.Contains("Standard");
    }

    private static bool LooksLikeParticleMaterial(Material material)
    {
        var shaderName = GetShaderName(material);
        return shaderName.StartsWith("Particles/") || shaderName.Contains("Particle");
    }

    private static bool LooksLikeUnlitMaterial(Material material)
    {
        var shaderName = GetShaderName(material);
        return shaderName.StartsWith("Unlit/")
            || shaderName.StartsWith("Mobile/Unlit")
            || (!material.HasProperty("_Metallic") && !material.HasProperty("_MetallicGlossMap") && !material.HasProperty("_BumpMap"));
    }

    private static bool HasTransparentSurface(Material material)
    {
        if (material.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.Transparent)
        {
            return true;
        }

        if (material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f)
        {
            return true;
        }

        if (material.HasProperty("_Mode"))
        {
            return material.GetFloat("_Mode") > 0.5f;
        }

        var color = GetColor(material, "_BaseColor", "_Color");
        return color.a < 0.999f;
    }

    private static string GetShaderName(Material material)
    {
        return material.shader == null ? string.Empty : material.shader.name;
    }

    private static Texture GetTexture(Material material, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                return material.GetTexture(propertyName);
            }
        }

        return null;
    }

    private static Color GetColor(Material material, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                return material.GetColor(propertyName);
            }
        }

        return Color.white;
    }

    private static float GetFloat(Material material, float fallback, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                return material.GetFloat(propertyName);
            }
        }

        return fallback;
    }

    private static Vector2 GetTextureScale(Material material, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                return material.GetTextureScale(propertyName);
            }
        }

        return Vector2.one;
    }

    private static Vector2 GetTextureOffset(Material material, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                return material.GetTextureOffset(propertyName);
            }
        }

        return Vector2.zero;
    }

    private static void SetTextureIfPresent(Material material, string propertyName, Texture texture, Vector2 scale, Vector2 offset)
    {
        if (!material.HasProperty(propertyName))
        {
            return;
        }

        material.SetTexture(propertyName, texture);
        material.SetTextureScale(propertyName, scale);
        material.SetTextureOffset(propertyName, offset);
    }

    private static void SetColorIfPresent(Material material, string propertyName, Color value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, value);
        }
    }

    private static void SetFloatIfPresent(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private struct MaterialSnapshot
    {
        public Texture BaseMap;
        public Vector2 BaseMapScale;
        public Vector2 BaseMapOffset;
        public Texture NormalMap;
        public Vector2 NormalMapScale;
        public Vector2 NormalMapOffset;
        public Texture EmissionMap;
        public Vector2 EmissionMapScale;
        public Vector2 EmissionMapOffset;
        public Texture MetallicGlossMap;
        public Vector2 MetallicMapScale;
        public Vector2 MetallicMapOffset;
        public Texture OcclusionMap;
        public Vector2 OcclusionMapScale;
        public Vector2 OcclusionMapOffset;
        public Color BaseColor;
        public Color EmissionColor;
        public float Metallic;
        public float Smoothness;
        public float Cutoff;
        public float NormalStrength;
        public float OcclusionStrength;
        public float CullMode;
        public bool Transparent;
        public bool AlphaClip;
        public bool EmissionEnabled;
        public bool ReceiveShadows;

        public static MaterialSnapshot Capture(Material material)
        {
            var emissionColor = GetColor(material, "_EmissionColor");
            var emissionMap = GetTexture(material, "_EmissionMap");
            return new MaterialSnapshot
            {
                BaseMap = GetTexture(material, "_BaseMap", "_MainTex"),
                BaseMapScale = GetTextureScale(material, "_BaseMap", "_MainTex"),
                BaseMapOffset = GetTextureOffset(material, "_BaseMap", "_MainTex"),
                NormalMap = GetTexture(material, "_BumpMap"),
                NormalMapScale = GetTextureScale(material, "_BumpMap"),
                NormalMapOffset = GetTextureOffset(material, "_BumpMap"),
                EmissionMap = emissionMap,
                EmissionMapScale = GetTextureScale(material, "_EmissionMap"),
                EmissionMapOffset = GetTextureOffset(material, "_EmissionMap"),
                MetallicGlossMap = GetTexture(material, "_MetallicGlossMap"),
                MetallicMapScale = GetTextureScale(material, "_MetallicGlossMap"),
                MetallicMapOffset = GetTextureOffset(material, "_MetallicGlossMap"),
                OcclusionMap = GetTexture(material, "_OcclusionMap"),
                OcclusionMapScale = GetTextureScale(material, "_OcclusionMap"),
                OcclusionMapOffset = GetTextureOffset(material, "_OcclusionMap"),
                BaseColor = GetColor(material, "_BaseColor", "_Color"),
                EmissionColor = emissionColor,
                Metallic = GetFloat(material, 0f, "_Metallic"),
                Smoothness = GetFloat(material, 0.5f, "_Smoothness", "_Glossiness"),
                Cutoff = GetFloat(material, 0.5f, "_Cutoff"),
                NormalStrength = GetFloat(material, 1f, "_BumpScale"),
                OcclusionStrength = GetFloat(material, 1f, "_OcclusionStrength"),
                CullMode = GetFloat(material, 2f, "_Cull"),
                Transparent = HasTransparentSurface(material),
                AlphaClip = GetFloat(material, 0f, "_AlphaClip") > 0.5f || GetFloat(material, 0f, "_Mode") == 1f,
                EmissionEnabled = emissionMap != null || emissionColor.maxColorComponent > 0.001f,
                ReceiveShadows = GetFloat(material, 1f, "_ReceiveShadows") > 0.5f
            };
        }
    }
}
