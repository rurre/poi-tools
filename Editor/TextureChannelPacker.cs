#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Poi
{
    public class TextureChannelPacker : EditorWindow
    {
        readonly int[] sizes = { 128, 256, 512, 1024, 2048, 4096 };
        int Size = 1024;

        Texture2D red, green, blue, alpha, unpack;

        enum Tab { Pack, Unpack }
        int selectedTab = 0;
        string[] TabNames
        {
            get
            {
                if(_tabNames == null)
                    _tabNames = Enum.GetNames(typeof(Tab));
                return _tabNames;
            }
        }

        Shader PackerShader
        {
            get
            {
                if(!_packerShader)
                    _packerShader = Shader.Find("Hidden/Poi/TexturePacker");
                return _packerShader;
            }
        }

        [MenuItem("Poi/Tools/Channel Packer")]
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow editorWindow = GetWindow(typeof(TextureChannelPacker));
            editorWindow.autoRepaintOnSceneChange = true;

            editorWindow.Show();
            editorWindow.titleContent = new GUIContent("Channel Packer");
        }

        private void OnGUI()
        {
            PoiHelpers.DrawLine();
            selectedTab = GUILayout.Toolbar(selectedTab, TabNames);
            PoiHelpers.DrawLine();

            if(selectedTab == (int)Tab.Pack)
                DrawPackUI();
            else
                DrawUnpackUI();
        }

        Texture2D preview;

        private void DrawPackUI()
        {
            red = EditorGUILayout.ObjectField("Red", red, typeof(Texture2D), true) as Texture2D;
            green = EditorGUILayout.ObjectField("Green", green, typeof(Texture2D), true) as Texture2D;
            blue = EditorGUILayout.ObjectField("Blue", blue, typeof(Texture2D), true) as Texture2D;
            alpha = EditorGUILayout.ObjectField("Alpha", alpha, typeof(Texture2D), true) as Texture2D;

            PoiHelpers.DrawLine();

            if(GUILayout.Button("Pack"))
            {
                var tex = PackTexture(Size, red, green, blue, alpha);
                if(tex)
                    tex.SaveTextureAsset("Assets/tex.png", true);
            }
        }

        private void DrawUnpackUI()
        {
            unpack = EditorGUILayout.ObjectField("To Unpack", unpack, typeof(Texture2D), true) as Texture2D;

            PoiHelpers.DrawLine();

            EditorGUI.BeginDisabledGroup(!unpack);

            if(GUILayout.Button("Unpack"))
            {
                UnpackTexture(unpack);
            }

            EditorGUI.EndDisabledGroup();

            PoiHelpers.DrawLine();
        }

        Texture2D PackTexture(int size, Texture2D red, Texture2D green, Texture2D blue, Texture2D alpha)
        {
            if(!PackerShader)
            {
                Debug.LogWarning("Packer shader is missing or invalid. Can't pack textures.");
                return null;
            }

            // Setup Material
            var mat = new Material(PackerShader);

            mat.SetTexture("_Red", red);
            mat.SetTexture("_Green", green);
            mat.SetTexture("_Blue", blue);
            mat.SetTexture("_Alpha", alpha);

            // Create texture and render to it
            var tex = new Texture2D(size, size);
            tex.BakeMaterialToTexture(mat);

            // Cleanup
            PoiHelpers.DestroyAppropriate(mat);

            return tex;
        }

        Texture2D[] UnpackTexture(Texture2D packed)
        {
            var red = new CustomRenderTexture(Size, Size, RenderTextureFormat.Default);
            var green = new CustomRenderTexture(Size, Size, RenderTextureFormat.Default);
            var blue = new CustomRenderTexture(Size, Size, RenderTextureFormat.Default);
            var alpha = new CustomRenderTexture(Size, Size, RenderTextureFormat.Default);

            return null;
        }

        string[] _tabNames;
        Shader _packerShader;
    }
}
#endif