using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace Poi
{
    public class BakeToVertexColorsEditor : EditorWindow
    {
        static GameObject selection = null;

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

            selection = EditorGUILayout.ObjectField("Object", selection, typeof(GameObject), true) as GameObject;

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(!selection);
            if(GUILayout.Button("Bake Normals to Vertex Colors"))
            {
                var meshes = GetAllMeshesInfo(selection);
                BakeAveragedNormalsToColors(meshes);
            }

            if(GUILayout.Button("Bake Vertex Positions to Vertex Colors"))
            {
                var meshes = GetAllMeshesInfo(selection);
                BakePositionsToColors(meshes);
            }
            EditorGUI.EndDisabledGroup();
        }

        MeshInfo[] GetAllMeshesInfo(GameObject obj)
        {
            return GetAllMeshesInfo(obj.GetComponentsInChildren<Renderer>(true));
        }

        MeshInfo[] GetAllMeshesInfo(params Renderer[] renderers)
        {
            var infos = renderers.Select(ren =>
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

        void BakePositionsToColors(MeshInfo[] infos)
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
        }

        void BakeAveragedNormalsToColors(params MeshInfo[] infos)
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
    }


}
