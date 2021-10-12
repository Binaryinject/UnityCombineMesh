using System;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using NaughtyAttributes;
using XLua;
#if UNITY_EDITOR
using UnityEditor;
[CanEditMultipleObjects]
#endif
[LuaCallCSharp]
public class CombineMeshes : MonoBehaviour
{
    public List<Transform> excluded = new List<Transform>();
    public bool CombineOnStart = true;
    [HideIf("VertexFlag")]
    [ProgressBar("Vertex Count", 65536, EColor.Green)]
    public int vertexCount = 0;

    [ShowIf("VertexFlag")]
    [SerializeField]
    [ProgressBar("Max Vertex Count", 65536, EColor.Red)]
    private int vertexOver = 0;
    
    private bool VertexFlag ()
    {
        vertexOver = vertexCount;
        return vertexCount > 65536;
    }

    private void Reset()
    {
        CalcVertexCount();
    }

    private void Start()
    {
        if (CombineOnStart) CombineMeshProcess();
    }

    [Button]
    void CalcVertexCount()
    {
        vertexCount = 0;
        var mfChildren = GetComponentsInChildren<MeshFilter>();
        foreach (var mfChild in mfChildren)
        {
            if (mfChild.transform == transform) continue;
            vertexCount += mfChild.sharedMesh.vertexCount;
        }
    }

    [Button]
    void CombineRuntime()
    {
        CombineMeshProcess();
    }

    [Button]
    public void CombineRestore()
    {
        if (GetComponent<MeshFilter>()) DestroyImmediate(GetComponent<MeshFilter>());
        if (GetComponent<MeshRenderer>()) DestroyImmediate(GetComponent<MeshRenderer>());
        if (GetComponent<MeshCollider>()) DestroyImmediate(GetComponent<MeshCollider>());
        var mfChildren = GetComponentsInChildren<MeshRenderer>();
        foreach (var mfChild in mfChildren)
        {
            mfChild.enabled = true;
        }

        excluded.Clear();
    }

    public bool CombineMeshProcess()
    {
        var oldPos = transform.position;
        transform.position = Vector3.zero;
        var mfChildren = GetComponentsInChildren<MeshFilter>();
        if (mfChildren.Length == 0) return false;

        var mrChildren = GetComponentsInChildren<MeshRenderer>();

        var mrSelf = gameObject.GetComponent<MeshRenderer>();
        var mfSelf = gameObject.GetComponent<MeshFilter>();

        if (!mrSelf || !mfSelf)
        {
            mrSelf = gameObject.AddComponent<MeshRenderer>();
            mfSelf = gameObject.AddComponent<MeshFilter>();
        }

        var combineMats = new List<Material>();
        foreach (var render in mrChildren)
        {
            if (render.transform == transform)
                continue;
            var localMats = render.sharedMaterials;
            foreach (var mat in localMats)
            {
                if (!combineMats.Contains(mat))
                {
                    combineMats.Add(mat);
                }
            }
        }

        //提取submesh
        var subMeshs = new List<Mesh>();
        foreach (var material in combineMats)
        {
            var combines = new List<CombineInstance>();
            for (int i = 0; i < mfChildren.Length; i++)
            {
                if (mfChildren[i].transform == transform || excluded.Contains(mfChildren[i].transform))
                {
                    continue;
                }

                var localMats = mfChildren[i].GetComponent<MeshRenderer>().sharedMaterials;
                for (int j = 0; j < localMats.Length; j++)
                {
                    if (localMats[j] != material)
                    {
                        continue;
                    }

                    var ci = new CombineInstance();
                    ci.mesh = mfChildren[i].sharedMesh;
                    ci.transform = mfChildren[i].transform.localToWorldMatrix;
                    ci.subMeshIndex = j;
                    mrChildren[i].enabled = false;
                    combines.Add(ci);
                }
            }

            var subMesh = new Mesh();
            subMesh.CombineMeshes(combines.ToArray(), true, true);
            subMeshs.Add(subMesh);
        }

        //合并submesh
        var finalCombiners = new List<CombineInstance>();
        foreach (var t in subMeshs)
        {
            var ci = new CombineInstance();
            ci.mesh = t;
            ci.transform = Matrix4x4.identity;
            finalCombiners.Add(ci);
        }

        var newMesh = new Mesh();
        newMesh.CombineMeshes(finalCombiners.ToArray(), false); //合并submesh网格 
        mfSelf.mesh = newMesh;
        foreach (var ex in excluded)
        {
            foreach (var material in ex.GetComponent<Renderer>().materials)
            {
                combineMats.Remove(material);
            }
        }

        mrSelf.sharedMaterials = combineMats.ToArray();
        mrSelf.enabled = true;

        transform.position = oldPos;
        return true;
    }
}