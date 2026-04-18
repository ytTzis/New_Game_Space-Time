using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class StandardToUrpLitConverter
{
    private const string UrpLitShaderName = "Universal Render Pipeline/Lit";
    private const string UrpParticlesLitShaderName = "Universal Render Pipeline/Particles/Lit";
    private const string UrpParticlesUnlitShaderName = "Universal Render Pipeline/Particles/Unlit";

    [MenuItem("Tools/Rendering/Convert Standard Materials To URP Lit/Convert Selected Folders")]
    private static void ConvertSelectedFolders()
    {
        var folders = Selection.assetGUIDs
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(AssetDatabase.IsValidFolder)
            .Distinct()
            .ToArray();

        if (folders.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Convert Standard Materials",
                "Please select one or more folders in the Project window first.",
                "OK");
            return;
        }

        ConvertInFolders(folders);
    }

    [MenuItem("Tools/Rendering/Convert Standard Materials To URP Lit/Convert Whole Project")]
    private static void ConvertWholeProject()
    {
        ConvertInFolders(new[] { "Assets" });
    }

    [MenuItem("Tools/Rendering/Convert Built-in Particle Materials To URP/Convert Selected Folders")]
    private static void ConvertParticleSelectedFolders()
    {
        var folders = Selection.assetGUIDs
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(AssetDatabase.IsValidFolder)
            .Distinct()
            .ToArray();

        if (folders.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Convert Particle Materials",
                "Please select one or more folders in the Project window first.",
                "OK");
            return;
        }

        ConvertParticlesInFolders(folders);
    }

    [MenuItem("Tools/Rendering/Convert Built-in Particle Materials To URP/Convert Whole Project")]
    private static void ConvertParticleWholeProject()
    {
        ConvertParticlesInFolders(new[] { "Assets" });
    }

    private static void ConvertInFolders(string[] folders)
    {
        var shader = Shader.Find(UrpLitShaderName);
        if (shader == null)
        {
            EditorUtility.DisplayDialog(
                "Convert Standard Materials",
                "Could not find URP/Lit shader. Make sure URP is installed.",
                "OK");
            return;
        }

        var paths = new HashSet<string>();
        foreach (var folder in folders)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        var converted = 0;
        var skipped = 0;

        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (var path in paths.OrderBy(p => p))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || !ShouldConvert(material))
                {
                    skipped++;
                    continue;
                }

                var snapshot = Snapshot.Capture(material);
                material.shader = shader;
                ApplySnapshot(material, snapshot);
                EditorUtility.SetDirty(material);
                converted++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Standard->URP] Converted: {converted}, skipped: {skipped}");
        EditorUtility.DisplayDialog(
            "Convert Standard Materials",
            $"Done.\nConverted: {converted}\nSkipped: {skipped}",
            "OK");
    }

    private static void ConvertParticlesInFolders(string[] folders)
    {
        var litShader = Shader.Find(UrpParticlesLitShaderName);
        var unlitShader = Shader.Find(UrpParticlesUnlitShaderName);
        if (litShader == null && unlitShader == null)
        {
            EditorUtility.DisplayDialog(
                "Convert Particle Materials",
                "Could not find URP particle shaders. Make sure URP is installed.",
                "OK");
            return;
        }

        var paths = new HashSet<string>();
        foreach (var folder in folders)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        var converted = 0;
        var skipped = 0;

        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (var path in paths.OrderBy(p => p))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || !ShouldConvertParticle(material))
                {
                    skipped++;
                    continue;
                }

                var snapshot = Snapshot.Capture(material);
                material.shader = snapshot.Transparent
                    ? (unlitShader ?? litShader)
                    : (litShader ?? unlitShader);

                ApplyParticleSnapshot(material, snapshot);
                EditorUtility.SetDirty(material);
                converted++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Particles->URP] Converted: {converted}, skipped: {skipped}");
        EditorUtility.DisplayDialog(
            "Convert Particle Materials",
            $"Done.\nConverted: {converted}\nSkipped: {skipped}",
            "OK");
    }

    private static bool ShouldConvert(Material material)
    {
        var shader = material.shader;
        if (shader == null)
        {
            return false;
        }

        var shaderName = shader.name ?? string.Empty;
        if (shaderName.StartsWith("Universal Render Pipeline/"))
        {
            return false;
        }

        if (shaderName.StartsWith("MK4/"))
        {
            return false;
        }

        return shaderName == "Standard"
            || shaderName == "Standard (Specular setup)"
            || shaderName == "Autodesk Interactive"
            || shaderName.StartsWith("Legacy Shaders/");
    }

    private static bool ShouldConvertParticle(Material material)
    {
        var shader = material.shader;
        if (shader == null)
        {
            return false;
        }

        var shaderName = shader.name ?? string.Empty;
        if (shaderName.StartsWith("Universal Render Pipeline/Particles/"))
        {
            return false;
        }

        return shaderName.StartsWith("Particles/")
            || shaderName == "Mobile/Particles/Alpha Blended"
            || shaderName == "Mobile/Particles/Additive"
            || shaderName == "Mobile/Particles/Multiply";
    }

    private static void ApplySnapshot(Material material, Snapshot snapshot)
    {
        SetTexture(material, "_BaseMap", snapshot.BaseMap, snapshot.BaseMapScale, snapshot.BaseMapOffset);
        SetTexture(material, "_BumpMap", snapshot.NormalMap, snapshot.NormalMapScale, snapshot.NormalMapOffset);
        SetTexture(material, "_EmissionMap", snapshot.EmissionMap, snapshot.EmissionMapScale, snapshot.EmissionMapOffset);
        SetTexture(material, "_MetallicGlossMap", snapshot.MetallicGlossMap, snapshot.MetallicMapScale, snapshot.MetallicMapOffset);
        SetTexture(material, "_OcclusionMap", snapshot.OcclusionMap, snapshot.OcclusionMapScale, snapshot.OcclusionMapOffset);

        SetColor(material, "_BaseColor", snapshot.BaseColor);
        SetColor(material, "_EmissionColor", snapshot.EmissionColor);
        SetFloat(material, "_BumpScale", snapshot.BumpScale);
        SetFloat(material, "_Metallic", snapshot.Metallic);
        SetFloat(material, "_Smoothness", snapshot.Smoothness);
        SetFloat(material, "_Cutoff", snapshot.Cutoff);
        SetFloat(material, "_OcclusionStrength", snapshot.OcclusionStrength);
        SetFloat(material, "_Cull", snapshot.Cull);

        material.SetFloat("_Surface", snapshot.Transparent ? 1f : 0f);
        material.SetFloat("_AlphaClip", snapshot.AlphaClip ? 1f : 0f);
        material.SetOverrideTag("RenderType", snapshot.Transparent ? "Transparent" : "Opaque");
        material.renderQueue = snapshot.AlphaClip
            ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest
            : snapshot.Transparent
                ? (int)UnityEngine.Rendering.RenderQueue.Transparent
                : -1;

        if (snapshot.Transparent)
        {
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
        }
        else
        {
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            material.SetFloat("_ZWrite", 1f);
        }

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
    }

    private static void ApplyParticleSnapshot(Material material, Snapshot snapshot)
    {
        SetTexture(material, "_BaseMap", snapshot.BaseMap, snapshot.BaseMapScale, snapshot.BaseMapOffset);
        SetTexture(material, "_BumpMap", snapshot.NormalMap, snapshot.NormalMapScale, snapshot.NormalMapOffset);
        SetTexture(material, "_EmissionMap", snapshot.EmissionMap, snapshot.EmissionMapScale, snapshot.EmissionMapOffset);

        SetColor(material, "_BaseColor", snapshot.BaseColor);
        SetColor(material, "_EmissionColor", snapshot.EmissionColor);
        SetFloat(material, "_Cutoff", snapshot.Cutoff);
        SetFloat(material, "_BumpScale", snapshot.BumpScale);
        SetFloat(material, "_Smoothness", snapshot.Smoothness);
        SetFloat(material, "_Cull", snapshot.Cull);
        SetFloat(material, "_Surface", snapshot.Transparent ? 1f : 0f);
        SetFloat(material, "_AlphaClip", snapshot.AlphaClip ? 1f : 0f);
        SetFloat(material, "_Blend", 0f);
        SetFloat(material, "_SrcBlend", snapshot.Transparent
            ? (float)UnityEngine.Rendering.BlendMode.SrcAlpha
            : (float)UnityEngine.Rendering.BlendMode.One);
        SetFloat(material, "_DstBlend", snapshot.Transparent
            ? (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha
            : (float)UnityEngine.Rendering.BlendMode.Zero);
        SetFloat(material, "_ZWrite", snapshot.Transparent ? 0f : 1f);

        material.SetOverrideTag("RenderType", snapshot.Transparent ? "Transparent" : "Opaque");
        material.renderQueue = snapshot.Transparent
            ? (int)UnityEngine.Rendering.RenderQueue.Transparent
            : -1;

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
        }
        else
        {
            material.DisableKeyword("_EMISSION");
        }
    }

    private static void SetTexture(Material material, string property, Texture texture, Vector2 scale, Vector2 offset)
    {
        if (!material.HasProperty(property))
        {
            return;
        }

        material.SetTexture(property, texture);
        material.SetTextureScale(property, scale);
        material.SetTextureOffset(property, offset);
    }

    private static void SetColor(Material material, string property, Color value)
    {
        if (material.HasProperty(property))
        {
            material.SetColor(property, value);
        }
    }

    private static void SetFloat(Material material, string property, float value)
    {
        if (material.HasProperty(property))
        {
            material.SetFloat(property, value);
        }
    }

    private static Texture GetTexture(Material material, params string[] names)
    {
        foreach (var name in names)
        {
            if (material.HasProperty(name))
            {
                return material.GetTexture(name);
            }
        }

        return null;
    }

    private static Vector2 GetScale(Material material, params string[] names)
    {
        foreach (var name in names)
        {
            if (material.HasProperty(name))
            {
                return material.GetTextureScale(name);
            }
        }

        return Vector2.one;
    }

    private static Vector2 GetOffset(Material material, params string[] names)
    {
        foreach (var name in names)
        {
            if (material.HasProperty(name))
            {
                return material.GetTextureOffset(name);
            }
        }

        return Vector2.zero;
    }

    private static Color GetColor(Material material, params string[] names)
    {
        foreach (var name in names)
        {
            if (material.HasProperty(name))
            {
                return material.GetColor(name);
            }
        }

        return Color.white;
    }

    private static float GetFloat(Material material, float fallback, params string[] names)
    {
        foreach (var name in names)
        {
            if (material.HasProperty(name))
            {
                return material.GetFloat(name);
            }
        }

        return fallback;
    }

    private struct Snapshot
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
        public float BumpScale;
        public float Cutoff;
        public float OcclusionStrength;
        public float Cull;
        public bool Transparent;
        public bool AlphaClip;
        public bool EmissionEnabled;

        public static Snapshot Capture(Material material)
        {
            var emissionColor = GetColor(material, "_EmissionColor");
            var emissionMap = GetTexture(material, "_EmissionMap");
            var color = GetColor(material, "_BaseColor", "_Color");
            var mode = GetFloat(material, 0f, "_Mode");
            var surface = GetFloat(material, 0f, "_Surface");
            var transparent = material.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.Transparent
                || surface > 0.5f
                || mode > 0.5f
                || color.a < 0.999f;

            return new Snapshot
            {
                BaseMap = GetTexture(material, "_BaseMap", "_MainTex"),
                BaseMapScale = GetScale(material, "_BaseMap", "_MainTex"),
                BaseMapOffset = GetOffset(material, "_BaseMap", "_MainTex"),
                NormalMap = GetTexture(material, "_BumpMap"),
                NormalMapScale = GetScale(material, "_BumpMap"),
                NormalMapOffset = GetOffset(material, "_BumpMap"),
                EmissionMap = emissionMap,
                EmissionMapScale = GetScale(material, "_EmissionMap"),
                EmissionMapOffset = GetOffset(material, "_EmissionMap"),
                MetallicGlossMap = GetTexture(material, "_MetallicGlossMap"),
                MetallicMapScale = GetScale(material, "_MetallicGlossMap"),
                MetallicMapOffset = GetOffset(material, "_MetallicGlossMap"),
                OcclusionMap = GetTexture(material, "_OcclusionMap"),
                OcclusionMapScale = GetScale(material, "_OcclusionMap"),
                OcclusionMapOffset = GetOffset(material, "_OcclusionMap"),
                BaseColor = color,
                EmissionColor = emissionColor,
                Metallic = GetFloat(material, 0f, "_Metallic"),
                Smoothness = GetFloat(material, 0.5f, "_Smoothness", "_Glossiness"),
                BumpScale = GetFloat(material, 1f, "_BumpScale"),
                Cutoff = GetFloat(material, 0.5f, "_Cutoff"),
                OcclusionStrength = GetFloat(material, 1f, "_OcclusionStrength"),
                Cull = GetFloat(material, 2f, "_Cull"),
                Transparent = transparent,
                AlphaClip = GetFloat(material, 0f, "_AlphaClip") > 0.5f || mode == 1f,
                EmissionEnabled = emissionMap != null || emissionColor.maxColorComponent > 0.001f
            };
        }
    }
}
