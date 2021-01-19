using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor;
using UnityEngine;


namespace Poi
{
    public class BakeToVertexColorsEditor : EditorWindow
    {
        const string LOG_PREFIX = "Poi: ";
        static GameObject Selection
        {
            get => _selection;
            set
            {
                if(_selection == value)
                    return;
                _selection = value;

                RefreshHasVertexColors();
            }
        }

        static bool SelectionHasVertexColors { get; set; }

        static bool ShouldCreateBackup { get; set; }



        [MenuItem("Poi/Tools/Bake Vertex Colors")]
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow editorWindow = GetWindow(typeof(BakeToVertexColorsEditor));
            editorWindow.autoRepaintOnSceneChange = true;

            editorWindow.Show();
            editorWindow.titleContent = new GUIContent("Bake Colors");
        }

        void OnGUI()
        {
            GUILayout.Space(18f);

            EditorGUI.BeginChangeCheck();
            GameObject obj = EditorGUILayout.ObjectField("Avatar", Selection, typeof(GameObject), true) as GameObject;
            if(EditorGUI.EndChangeCheck())
                Selection = obj;

            DrawLine();

            EditorGUI.BeginDisabledGroup(!Selection);
            {
                EditorGUILayout.HelpBox("Use this if you want seamless outlines", MessageType.Info);
                if(GUILayout.Button("Bake Averaged Normals"))
                {
                    var meshes = GetAllMeshInfos(Selection);
                    if(ShouldCreateBackup)
                        BackupAvatar(Selection);
                    BakeAveragedNormalsToColors(meshes);
                    RefreshHasVertexColors();
                }

                DrawLine(true, false);
                EditorGUILayout.HelpBox("Use this if you want scrolling emission", MessageType.Info);
                if(GUILayout.Button("Bake Vertex Positions"))
                {
                    var meshes = GetAllMeshInfos(Selection);
                    if(ShouldCreateBackup)
                        BackupAvatar(Selection);
                    BakePositionsToColors(meshes);
                    RefreshHasVertexColors();
                }
            }
            EditorGUI.EndDisabledGroup();

            DrawLine(true, false);

            if(!Selection || !SelectionHasVertexColors)
                return;

            EditorGUILayout.HelpBox("Your mesh already has vertex colors assigned.\nBaking new ones will overwrite them.\n\nNote: You can only have one set of vertex colors at a time", MessageType.Warning);
            DrawLine(false, false);
            ShouldCreateBackup = EditorGUILayout.ToggleLeft("Create avatar backup before baking", ShouldCreateBackup);
            DrawLine(false, false);
        }

        static void BackupAvatar(GameObject selection)
        {
            if(!SelectionHasVertexColors)
                return;
            string assetPath = AssetDatabase.GetAssetPath(selection);
            if(string.IsNullOrWhiteSpace(assetPath))
                return;

            string ext = Path.GetExtension(assetPath);
            string pathNoExt = Path.Combine(Path.GetDirectoryName(assetPath), Path.GetFileNameWithoutExtension(assetPath));
            string newPath = AssetDatabase.GenerateUniqueAssetPath($"{pathNoExt}_backup{ext}");

            AssetDatabase.CopyAsset(assetPath, newPath);
            Debug.Log(LOG_PREFIX + "Backed up avatar at " + newPath);
        }

        static MeshInfo[] GetAllMeshInfos(GameObject obj)
        {
            return GetAllMeshInfos(obj?.GetComponentsInChildren<Renderer>(true));
        }

        static MeshInfo[] GetAllMeshInfos(params Renderer[] renderers)
        {
            var infos = renderers?.Select(ren =>
            {
                MeshInfo info = new MeshInfo();
                if(ren is SkinnedMeshRenderer smr)
                {
                    Mesh bakedMesh = new Mesh();
                    Transform tr = smr.gameObject.transform;
                    Quaternion origRot = tr.localRotation;
                    Vector3 origScale = tr.localScale;

                    tr.localRotation = Quaternion.identity;
                    tr.localScale = Vector3.one;

                    smr.BakeMesh(bakedMesh);

                    tr.localRotation = origRot;
                    tr.localScale = origScale;

                    info.sharedMesh = smr.sharedMesh;
                    info.bakedVertices = bakedMesh?.vertices;
                    info.bakedNormals = bakedMesh?.normals;
                }
                else if(ren is MeshRenderer mr)
                {
                    info.sharedMesh = mr.GetComponent<MeshFilter>()?.sharedMesh;
                    info.bakedVertices = info.sharedMesh?.vertices;
                    info.bakedNormals = info.sharedMesh?.normals;
                }
                return info;
            }).ToArray();

            return infos;
        }

        static void BakePositionsToColors(MeshInfo[] infos)
        {
            foreach(var info in infos)
            {
                if(info.sharedMesh == null)
                    continue;

                Vector3[] verts = info.bakedVertices;    //accessing mesh.vertices on every iteration is very slow
                Color[] colors = new Color[verts.Length];
                for(int i = 0; i < verts.Length; i++)
                    colors[i] = new Color(verts[i].x, verts[i].y, verts[i].z);
                info.sharedMesh.colors = colors;
            }
            Debug.Log(LOG_PREFIX + "Finished baking vertex positions to vertex colors");
        }

        static void BakeAveragedNormalsToColors(params MeshInfo[] infos)
        {
            foreach(var meshInfo in infos)
            {
                Vector3[] verts = meshInfo.bakedVertices;
                Vector3[] normals = meshInfo.bakedNormals;
                VertexInfo[] vertInfo = new VertexInfo[verts.Length];
                for(int i = 0; i < verts.Length; i++)
                {
                    vertInfo[i] = new VertexInfo()
                    {
                        vertex = verts[i],
                        originalIndex = i,
                        normal = normals[i]
                    };
                }
                var groups = vertInfo.GroupBy(x => x.vertex);
                VertexInfo[] processedVertInfo = new VertexInfo[vertInfo.Length];
                int index = 0;
                foreach(IGrouping<Vector3, VertexInfo> group in groups)
                {
                    Vector3 avgNormal = Vector3.zero;
                    foreach(VertexInfo item in group)
                        avgNormal += item.normal;

                    avgNormal /= group.Count();
                    foreach(VertexInfo item in group)
                    {
                        processedVertInfo[index] = new VertexInfo()
                        {
                            vertex = item.vertex,
                            originalIndex = item.originalIndex,
                            normal = item.normal,
                            averagedNormal = avgNormal
                        };
                        index++;
                    }
                }
                Color[] colors = new Color[verts.Length];
                for(int i = 0; i < processedVertInfo.Length; i++)
                {
                    VertexInfo info = processedVertInfo[i];

                    int origIndex = info.originalIndex;
                    Vector3 normal = info.averagedNormal;
                    Color normColor = new Color(normal.x, normal.y, normal.z, 1);
                    colors[origIndex] = normColor;
                }
                meshInfo.sharedMesh.colors = colors;
            }
            Debug.Log(LOG_PREFIX + "Finished baking averaged normals to vertex colors");
        }

        static void DrawLine(bool spaceBefore = true, bool spaceAfter = true)
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

        static void RefreshHasVertexColors()
        {
            var meshes = GetAllMeshInfos(Selection).Select(i => i.sharedMesh);
            SelectionHasVertexColors = meshes
                .SelectMany(r => r.colors)
                .Any(c => c != Color.white);
        }

        struct MeshInfo
        {
            public Mesh sharedMesh;
            public Vector3[] bakedVertices;
            public Vector3[] bakedNormals;
        }

        struct VertexInfo
        {
            public Vector3 vertex;
            public int originalIndex;
            public Vector3 normal;
            public Vector3 averagedNormal;
        }

        static GameObject _selection;
        static bool _selectionHasVertexColors;
    }
}
