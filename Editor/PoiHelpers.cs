#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

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
        /// Extension method that bakes a material to <paramref name="tex"/>
        /// </summary>
        /// <param name="tex">Texture to bake <paramref name="materialToBake"/> to</param>
        /// <param name="materialToBake">Material to bake to <paramref name="tex"/></param>
        internal static void BakeMaterialToTexture(this Texture2D tex, Material materialToBake)
        {
            var res = new Vector2Int(tex.width, tex.height);

            RenderTexture renderTexture = RenderTexture.GetTemporary(res.x, res.y);
            Graphics.Blit(null, renderTexture, materialToBake);

            //transfer image from rendertexture to texture
            RenderTexture.active = renderTexture;
            tex.ReadPixels(new Rect(Vector2.zero, res), 0, 0);

            //clean up variables
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
                assetPath = AbsolutePathToLocalAssetsPath(assetPath);
            }
            else
            {
                string absolutePath = LocalAssetsPathToAbsolutePath(assetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            }

            if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) && !overwrite)
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            File.WriteAllBytes(assetPath, bytes);
            AssetDatabase.Refresh();
        }

        internal static Texture2D GetReadableTextureCopy(this Texture2D tex)
        {
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

        #endregion
    }
}
#endif