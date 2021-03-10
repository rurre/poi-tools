﻿#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Poi
{
    static class PoiHelpers
    {
        static readonly string suffixSeparator = "_";

        /// <summary>
        /// Changes a path in Assets to an absolute windows path
        /// </summary>
        /// <param name="localPath"></param>
        /// <returns></returns>
        public static string LocalAssetsPathToAbsolutePath(string localPath)
        {
            localPath = NormalizePathSlashes(localPath);
            const string assets = "Assets/";
            if(localPath.StartsWith(assets))
            {
                localPath = localPath.Remove(0, assets.Length);
                localPath = $"{Application.dataPath}/{localPath}";
            }
            return localPath;
        }

        /// <summary>
        /// Replaces all forward slashes \ with back slashes /
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string NormalizePathSlashes(string path)
        {
            if(!string.IsNullOrEmpty(path))
                path = path.Replace('\\', '/');
            return path;
        }

        /// <summary>
        /// Ensures directory exists inside the assets folder
        /// </summary>
        /// <param name="assetPath"></param>
        public static void EnsurePathExistsInAssets(string assetPath)
        {
            Directory.CreateDirectory(LocalAssetsPathToAbsolutePath(assetPath));
        }

        /// <summary>
        /// Adds a suffix to the end of the string then returns it
        /// </summary>
        /// <param name="str"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public static string AddSuffix(string str, string suffix)
        {
            return str + suffixSeparator + suffix;
        }

        /// <summary>
        /// Removes suffix from the end of string then returns it
        /// </summary>
        /// <param name="str"></param>
        /// <param name="suffixes">Each to be removed in order</param>
        /// <returns></returns>
        public static string RemoveSuffix(string str, params string[] suffixes)
        {
            foreach(string sfx in suffixes)
            {
                string s = suffixSeparator + sfx;
                if(!str.EndsWith(sfx))
                    continue;

                int idx = str.LastIndexOf(s);
                if(idx != -1)
                    str = str.Remove(idx, s.Length);
            }
            return str;
        }


        /// <summary>
        /// Draws a GUI ilne
        /// </summary>
        /// <param name="spaceBefore"></param>
        /// <param name="spaceAfter"></param>
        internal static void DrawLine(bool spaceBefore = true, bool spaceAfter = true)
        {
            float spaceHeight = 3f;
            if(spaceBefore)
                GUILayout.Space(spaceHeight);

            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));

            if(spaceAfter)
                GUILayout.Space(spaceHeight);
        }

        /// <summary>
        /// Destroys an object with DestroyImmediate in object mode and Destroy in play mode
        /// </summary>
        /// <param name="obj"></param>
        internal static void DestroyAppropriate(UnityEngine.Object obj)
        {
            if(EditorApplication.isPlaying)
                UnityEngine.Object.Destroy(obj);
            else
                UnityEngine.Object.DestroyImmediate(obj);
        }

        /// <summary>
        /// Changes path from full windows path to a local path in the Assets folder
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Path starting with Assets</returns>
        internal static string AbsolutePathToLocalAssetsPath(string path)
        {
            if(path.StartsWith(Application.dataPath))
                path = "Assets" + path.Substring(Application.dataPath.Length);
            return path;
        }

        #region Extensions
        /// <summary>
        /// Selects and highlights the asset in your unity Project tab
        /// </summary>
        /// <param name="path"></param>
        internal static void PingAssetAtPath(string path)
        {
            var inst = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path).GetInstanceID();
            EditorGUIUtility.PingObject(inst);
        }

        internal static Vector2Int DrawResolutionPicker(Vector2Int size, ref bool linked, ref bool autoDetect, int[] presets = null, string[] presetNames = null)
        {
            EditorGUI.BeginDisabledGroup(autoDetect);
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel("Size");

                EditorGUI.BeginChangeCheck();
                size.x = EditorGUILayout.IntField(size.x);
                if(linked && EditorGUI.EndChangeCheck())
                    size.y = size.x;

                EditorGUILayout.LabelField("x", GUILayout.MaxWidth(12));

                EditorGUI.BeginChangeCheck();
                size.y = EditorGUILayout.IntField(size.y);
                if(linked && EditorGUI.EndChangeCheck())
                    size.x = size.y;

                if(presets != null && presetNames != null)
                {
                    EditorGUI.BeginChangeCheck();
                    int selectedPresetIndex = EditorGUILayout.Popup(GUIContent.none, -1, presetNames, GUILayout.MaxWidth(16));
                    if(EditorGUI.EndChangeCheck() && selectedPresetIndex != -1)
                        size = new Vector2Int(presets[selectedPresetIndex], presets[selectedPresetIndex]);
                }

                linked = GUILayout.Toggle(linked, "L", EditorStyles.miniButton, GUILayout.MaxWidth(16));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();

            autoDetect = EditorGUILayout.Toggle("Auto detect", autoDetect);

            return size;
        }

        /// <summary>
        /// Gets the combined maximum width and height of the passed in textures
        /// </summary>
        /// <param name="textures"></param>
        /// <returns></returns>
        internal static Vector2Int GetMaxSizeFromTextures(params Texture2D[] textures)
        {
            var sizes = textures.Where(tex => tex).Select(tex => new Vector2Int(tex.width, tex.height)).ToArray();
            if(sizes.Length == 0)
                return default;

            int maxW = sizes.Max(wh => wh.x);
            int maxH = sizes.Max(wh => wh.y);
            return new Vector2Int(maxW, maxH);
        }

        internal static Texture2D PackTextures(Vector2Int resolution, Texture2D red, Texture2D green, Texture2D blue, Texture2D alpha, int invertRed = 0, int invertGreen = 0, int invertBlue = 0, int invertAlpha = 0)
        {
            // Setup Material
            var mat = new Material(PoiExtensions.PackerShader);

            mat.SetTexture("_Red", red);
            mat.SetTexture("_Green", green);
            mat.SetTexture("_Blue", blue);
            mat.SetTexture("_Alpha", alpha);

            mat.SetInt("_Invert_Red", invertRed);
            mat.SetInt("_Invert_Green", invertGreen);
            mat.SetInt("_Invert_Blue", invertBlue);
            mat.SetInt("_Invert_Alpha", invertAlpha);

            // Create texture and render to it
            var tex = new Texture2D(resolution.x, resolution.y);
            tex.BakeMaterialToTexture(mat);

            // Cleanup
            DestroyAppropriate(mat);

            return tex;
        }

        internal static Dictionary<string, Texture2D> UnpackTextureToChannels(Texture2D packedTexture, Vector2Int resolution)
        {
            var channels = new Dictionary<string, Texture2D>
            {
                {"red", new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, true)},
                {"green", new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, true)},
                {"blue", new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, true)},
                {"alpha", new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, true)}
            };

            var mat = new Material(PoiExtensions.UnpackerShader);
            mat.mainTexture = packedTexture;

            for(int i = 0; i < 4; i++)
            {
                mat.SetFloat("_Mode", i);
                channels.ElementAt(i).Value.BakeMaterialToTexture(mat);
            }

            return channels;
        }

        internal static void DrawWithLabelWidth(float width, Action action)
        {
            if(action == null)
                return;
            float old = EditorGUIUtility.labelWidth;
            action.Invoke();
            EditorGUIUtility.labelWidth = 0;
        }

        internal static PoiExtensions.PoiTextureChannel DrawChannelSelector(PoiExtensions.PoiTextureChannel currentSelection)
        {
            string[] labels = { "All", "Red", "Green", "Blue", "Alpha" };
            return (PoiExtensions.PoiTextureChannel)GUILayout.SelectionGrid((int)currentSelection, labels, labels.Length);
        }
    }

    internal static class PoiExtensions
    {
        public enum PoiTextureChannel { RGBA, Red, Green, Blue, Alpha }
        public static Shader PackerShader
        {
            get
            {
                return Shader.Find("Hidden/Poi/TexturePacker");
            }
        }
        public static Shader UnpackerShader
        {
            get
            {
                return Shader.Find("Hidden/Poi/TextureUnpacker");
            }
        }

        internal static Texture2D GetChannelAsTexture(this Texture2D tex, PoiTextureChannel chan, int invertChannel)
        {
            if(chan == PoiTextureChannel.RGBA)
                return tex;

            Material mat = new Material(UnpackerShader);
            mat.SetInt("_Invert", invertChannel);
            mat.SetFloat("_Mode", (int)chan);
            mat.mainTexture = tex;

            var newTex = new Texture2D(tex.width, tex.height, TextureFormat.RGB24, false, true);
            newTex.BakeMaterialToTexture(mat);

            return newTex;
        }

        /// <summary>
        /// Extension method that bakes a material to <paramref name="tex"/>
        /// </summary>
        /// <param name="tex">Texture to bake <paramref name="materialToBake"/> to</param>
        /// <param name="materialToBake">Material to bake to <paramref name="tex"/></param>
        internal static void BakeMaterialToTexture(this Texture2D tex, Material materialToBake)
        {
            var res = new Vector2Int(tex.width, tex.height);

            RenderTexture renderTexture = RenderTexture.GetTemporary(res.x, res.y);
            Graphics.Blit(null, renderTexture, materialToBake);

            //Transfer image from RenderTexture to Texture
            RenderTexture.active = renderTexture;
            tex.ReadPixels(new Rect(Vector2.zero, res), 0, 0, true);

            //Clean up variables
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(renderTexture);
        }

        internal static void SaveTextureAsset(this Texture2D tex, string assetPath, bool overwrite)
        {
            var bytes = tex.EncodeToPNG();


            // Ensure directory exists then convert path to local asset path
            if(!assetPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
                assetPath = PoiHelpers.AbsolutePathToLocalAssetsPath(assetPath);
            }
            else
            {
                string absolutePath = PoiHelpers.LocalAssetsPathToAbsolutePath(assetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            }

            if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) && !overwrite)
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            File.WriteAllBytes(assetPath, bytes);
            AssetDatabase.Refresh();
        }

        internal static Texture2D GetReadableTextureCopy(this Texture2D tex)
        {
            if(tex.isReadable)
                return tex;

            byte[] pix = tex.GetRawTextureData();
            Texture2D finalTex = new Texture2D(tex.width, tex.height, tex.format, false);
            finalTex.LoadRawTextureData(pix);
            finalTex.Apply();
            return finalTex;
        }

        internal static void PingAssetAtPath(string path)
        {
            var inst = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path).GetInstanceID();
            EditorGUIUtility.PingObject(inst);
        }

        internal static bool IsNullOrEmpty<T>(this T[] array)
        {
            return array == null || array.Length == 0;
        }

        #endregion
    }
}
#endif