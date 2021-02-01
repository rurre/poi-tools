#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Poi
{
    public class TextureChannelPackerEditor : EditorWindow
    {
        // Packing
        Texture2D packRed, packGreen, packBlue, packAlpha;
        readonly int[] sizes = { 128, 256, 512, 1024, 2048, 4096 };
        int Size = 1024;
        Shader PackerShader
        {
            get
            {
                return Shader.Find("Hidden/Poi/TexturePacker");
                //if(!_packerShader)
                //    _packerShader = Shader.Find("Hidden/Poi/TexturePacker");
                //return _packerShader;
            }
        }


        // Unpacking
        Texture2D unpackSource;
        Shader UnpackerShader
        {
            get
            {
                return Shader.Find("Hidden/Poi/TextureUnpacker");
                //if(!_packerShader)
                //    _packerShader = Shader.Find("Hidden/Poi/TextureUnpacker");
                //return _packerShader;
            }
        }

        // Tabs
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

        string savePath = "Assets/_ChannelPacker";
        string packedName = "packed";
        string unpackedName = "unpacked";


        [MenuItem("Poi/Tools/Texture Packer")]
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow editorWindow = GetWindow(typeof(TextureChannelPackerEditor));
            editorWindow.autoRepaintOnSceneChange = true;

            editorWindow.Show();
            editorWindow.titleContent = new GUIContent("Texture Packer");
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
            packRed = EditorGUILayout.ObjectField("Red", packRed, typeof(Texture2D), true) as Texture2D;
            packGreen = EditorGUILayout.ObjectField("Green", packGreen, typeof(Texture2D), true) as Texture2D;
            packBlue = EditorGUILayout.ObjectField("Blue", packBlue, typeof(Texture2D), true) as Texture2D;
            packAlpha = EditorGUILayout.ObjectField("Alpha", packAlpha, typeof(Texture2D), true) as Texture2D;

            PoiHelpers.DrawLine();

            bool disabled = new bool[] { packRed, packGreen, packBlue, packAlpha }.Count(b => b) < 2;

            EditorGUI.BeginDisabledGroup(disabled);
            {
                packedName = EditorGUILayout.TextField("File name", packedName);

                if(GUILayout.Button("Pack"))
                {
                    var packResult = PackTexture(Size, packRed, packGreen, packBlue, packAlpha);
                    if(packResult)
                    {
                        string path = $"{savePath}/Packed/{packedName}.png";
                        packResult.SaveTextureAsset(path, true);
                        PoiHelpers.PingAssetAtPath(path);
                    }
                }
            }
            PoiHelpers.DrawLine();
        }

        private void DrawUnpackUI()
        {
            unpackSource = EditorGUILayout.ObjectField("To Unpack", unpackSource, typeof(Texture2D), true) as Texture2D;

            PoiHelpers.DrawLine();

            EditorGUI.BeginDisabledGroup(!unpackSource);
            {
                unpackedName = EditorGUILayout.TextField("File name", unpackedName);

                if(GUILayout.Button("Unpack"))
                {
                    var output = UnpackTextureToChannels(unpackSource);
                    string pingPath = null;
                    try
                    {
                        AssetDatabase.StartAssetEditing();
                        foreach(var kv in output)
                        {
                            if(string.IsNullOrWhiteSpace(pingPath))
                                pingPath = $"{savePath}/Unpacked/{unpackedName}_{kv.Key}.png";
                            kv.Value?.SaveTextureAsset($"{savePath}/Unpacked/{unpackedName}_{kv.Key}.png", true);
                        }
                    }
                    catch {}
                    finally
                    {
                        AssetDatabase.StopAssetEditing();
                    }

                    PoiHelpers.PingAssetAtPath(pingPath);
                }
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

        Dictionary<string, Texture2D> UnpackTextureToChannels(Texture2D packedTexture)
        {
            var channels = new Dictionary<string, Texture2D>()
            {
                {"red", new Texture2D(Size, Size)},
                {"green", new Texture2D(Size, Size)},
                {"blue", new Texture2D(Size, Size)},
                {"alpha", new Texture2D(Size, Size)}
            };

            var mat = new Material(UnpackerShader);
            mat.SetTexture("_Packed", packedTexture);

            for(int i = 0; i < 4; i++)
            {
                mat.SetFloat("_Mode", i);
                channels.ElementAt(i).Value.BakeMaterialToTexture(mat);
            }

            return channels;
        }

        string[] _tabNames;
        Shader _packerShader;
    }
}
#endif