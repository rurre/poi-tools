using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TextureChannel = Poi.PoiExtensions.PoiTextureChannel;

namespace Poi
{
    public class TextureChannelPackerEditor : EditorWindow
    {
        const string LOG_PREFIX = "<color=blue>Poi:</color> "; //color is hex or name
        static readonly Vector2 MIN_WINDOW_SIZE = new Vector2(336, 590);
        const int AUTO_SELECT_CEILING = 2048;
        const float CHANNEL_LABEL_WIDTH = 40;

        // Default values
        string savePath = "Assets/_ChannelPacker";
        string packedName = "packed";
        string unpackedName = "unpacked";

        // Version
        Version version = new Version(1, 1);
        string SubTitle
        {
            get
            {
                if(string.IsNullOrWhiteSpace(_subTitle))
                    _subTitle = "by Pumkin - v" + version.ToString();
                return _subTitle;
            }
        }

        static EditorWindow Window
        {
            get
            {
                if(!_window)
                    _window = GetWindow<TextureChannelPackerEditor>();
                return _window;
            }
        }

        // Texture stuff
        static int[] SizePresets { get; } = { 128, 256, 512, 1024, 2048, 4096 };
        string[] SizePresetNames
        {
            get
            {
                if(_sizeNames == null)
                    _sizeNames = SizePresets.Select(i => i + " x " + i).ToArray();
                return _sizeNames;
            }
        }

        Vector2Int PackSize { get; set; } = new Vector2Int(1024, 1024);
        Vector2Int UnpackSize { get; set; } = new Vector2Int(1024, 1024);

        bool packSizeIsLinked = true;
        bool unpackSizeIsLinked = true;

        bool packSizeAutoSelect = true;
        bool unpackSizeAutoSelect = true;

        bool showChannelPicker = false;

        TextureChannel redTexChan, blueTexChan, greenTexChan, alphaTexChan;

        Texture2D packRed, packGreen, packBlue, packAlpha;
        Texture2D unpackSource;

        bool PackerShadersExist
        {
            get
            {
                bool everythingIsAlwaysFine = true;

                if(!PoiExtensions.UnpackerShader)
                {
                    Debug.LogWarning(LOG_PREFIX + "Unpacker shader is missing or invalid. Can't unpack textures.");
                    everythingIsAlwaysFine = false;
                }

                if(!PoiExtensions.PackerShader)
                {
                    Debug.LogWarning(LOG_PREFIX + "Packer shader is missing or invalid. Can't pack textures.");
                    everythingIsAlwaysFine = false;
                }

                return everythingIsAlwaysFine;
            }
        }

        // UI
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


        [MenuItem("Poi/Tools/Texture Packer", priority = 0)]
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            Window.autoRepaintOnSceneChange = true;
            Window.minSize = MIN_WINDOW_SIZE;

            Window.Show();
            Window.titleContent = new GUIContent("Texture Packer");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Poi Texture Packer", PoiStyles.TitleLabel);
            EditorGUILayout.LabelField(SubTitle);

            PoiHelpers.DrawLine();

            selectedTab = GUILayout.Toolbar(selectedTab, TabNames);

            if(selectedTab == (int)Tab.Pack)
                DrawPackUI();
            else
                DrawUnpackUI();
        }

        void DrawTextureSelector(string label, ref Texture2D tex)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                tex = EditorGUILayout.ObjectField(label, tex, typeof(Texture2D), true, GUILayout.ExpandHeight(true)) as Texture2D;
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawTextureSelector(string label, ref Texture2D tex, ref TextureChannel selectedChannel)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                tex = EditorGUILayout.ObjectField(label, tex, typeof(Texture2D), true, GUILayout.ExpandHeight(true)) as Texture2D;

                if(showChannelPicker)
                {
                    EditorGUI.BeginDisabledGroup(!tex);
                    selectedChannel = PoiHelpers.DrawChannelSelector(selectedChannel);
                    EditorGUI.EndDisabledGroup();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawPackUI()
        {
            EditorGUI.BeginChangeCheck();
            {
                DrawTextureSelector("Red", ref packRed, ref redTexChan);
                DrawTextureSelector("Green", ref packGreen, ref greenTexChan);
                DrawTextureSelector("Blue", ref packBlue, ref blueTexChan);
                DrawTextureSelector("Alpha", ref packAlpha, ref alphaTexChan);
            }
            if(EditorGUI.EndChangeCheck() && packSizeAutoSelect)
            {
                // Get biggest texture size from selections and make a selection in our sizes list
                var tempSize = PoiHelpers.GetMaxSizeFromTextures(packRed, packGreen, packBlue, packAlpha);
                if(tempSize != default)
                    PackSize = tempSize.ClosestPowerOfTwo(AUTO_SELECT_CEILING);
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            showChannelPicker = EditorGUILayout.ToggleLeft("Pick source channel", showChannelPicker);
            EditorGUILayout.EndHorizontal();

            bool disabled = new bool[] { packRed, packGreen, packBlue, packAlpha }.Count(b => b) < 2;

            EditorGUI.BeginDisabledGroup(disabled);
            {
                PackSize = DrawTextureSizeSettings(PackSize, ref packedName, ref packSizeIsLinked, ref packSizeAutoSelect);

                if(GUILayout.Button("Pack", PoiStyles.BigButton))
                    DoPack();

                EditorGUILayout.Space();
            }
            EditorGUI.EndDisabledGroup();
        }

        private Vector2Int DrawTextureSizeSettings(Vector2Int size, ref string fileName, ref bool sizeIsLinked, ref bool sizeAutoSelect)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                fileName = EditorGUILayout.TextField("File name", fileName);
                EditorGUILayout.Space();
                size = PoiHelpers.DrawResolutionPicker(size, ref sizeIsLinked, ref sizeAutoSelect, SizePresets, SizePresetNames);
            }
            EditorGUILayout.EndVertical();
            return size;
        }

        void DoPack()
        {
            if(PackerShadersExist)
            {
                Texture2D red = packRed;
                Texture2D green = packGreen;
                Texture2D blue = packBlue;
                Texture2D alpha = packAlpha;

                if(showChannelPicker)
                {
                    red = packRed.GetChannelAsTexture(redTexChan);
                    green = packRed.GetChannelAsTexture(greenTexChan);
                    blue = packRed.GetChannelAsTexture(blueTexChan);
                    alpha = packRed.GetChannelAsTexture(alphaTexChan);
                }

                Texture2D packResult = PoiHelpers.PackTextures(PackSize, red, green, blue, alpha);
                if(packResult)
                {
                    string path = $"{savePath}/Packed/{packedName}.png";
                    packResult.SaveTextureAsset(path, true);
                    Debug.Log(LOG_PREFIX + "Finished packing texture at " + path);
                    PoiHelpers.PingAssetAtPath(path);
                }
            }
        }

        void DrawUnpackUI()
        {
            EditorGUI.BeginChangeCheck();
            {
                DrawTextureSelector("Packed Texture", ref unpackSource);
            }
            if(EditorGUI.EndChangeCheck() && unpackSizeAutoSelect)
            {
                // Get biggest texture size from selections and make a selection in our sizes list
                var tempSize = PoiHelpers.GetMaxSizeFromTextures(unpackSource);
                if(tempSize != default)
                    UnpackSize = tempSize.ClosestPowerOfTwo(AUTO_SELECT_CEILING);
            }

            EditorGUI.BeginDisabledGroup(!unpackSource);
            {
                UnpackSize = DrawTextureSizeSettings(UnpackSize, ref unpackedName, ref unpackSizeIsLinked, ref unpackSizeAutoSelect);

                if(GUILayout.Button("Unpack", PoiStyles.BigButton))
                {
                    if(PackerShadersExist)
                    {
                        var channelTextures = PoiHelpers.UnpackTextureToChannels(unpackSource, UnpackSize);
                        string pingPath = null;
                        pingPath = SaveTextures(channelTextures, pingPath);

                        Debug.Log(LOG_PREFIX + "Finished unpacking texture at " + pingPath);
                        PoiHelpers.PingAssetAtPath(pingPath);
                    }
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
        }

        string SaveTextures(Dictionary<string, Texture2D> output, string pingPath)
        {
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
            catch { }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            return pingPath;
        }


        string[] _tabNames;
        Shader _packerShader;
        Shader _unpackerShader;
        string[] _sizeNames;
        private static EditorWindow _window;
        string _subTitle;
        private float _versionLabelWidth;
    }
}