using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    MarchingCubes _mcubes = new MarchingCubes();
    const float voxelSize = 1.0f;

    void Start()
    {
        int chunkSize = 10;
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                GenerateChunk(new Vector3(i*voxelSize*chunkSize, 0.0f, j*voxelSize*chunkSize), chunkSize);
            }
        }
    }

    GameObject GenerateChunk(Vector3 origin, int size)
    {
        _mcubes.Reset();
        for(int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                for (int k = 0; k < size; k++)
                {
                    Vector3 offset = new Vector3(i*voxelSize, j*voxelSize, k*voxelSize);
                    _mcubes.MarchCube(origin + offset, voxelSize);
                }
            }
        }
        var mesh = new Mesh();
        mesh.vertices = _mcubes.GetVertices();
        mesh.triangles = _mcubes.GetIndices();
        mesh.uv = new Vector2[mesh.vertices.Length];
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        var go = new GameObject("TerrainChunk");
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mf.mesh = mesh;
        mr.material = new Material(Shader.Find("Standard"));
        return go;
    }
}
