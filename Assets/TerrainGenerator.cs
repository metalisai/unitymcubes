using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class TerrainGenerator
{
    MarchingCubes _mcubes;
    Material sharedMaterial;

    float fSample(Vector3 position)
    {
        float pi = Mathf.PI;
        float sampleSize = 0.1f;

        float height01 = Mathf.PerlinNoise((position.x+pi)*sampleSize, (position.z+pi)*sampleSize);
        float height = height01 * 8.0f;
        float heightSample = height - position.y;

        float volumetricSample = PerlinNoise.PerlinNoise3((position.x+pi)*sampleSize, (position.y+pi)*sampleSize, (position.z+pi)*sampleSize);
        return Mathf.Min(heightSample, -volumetricSample) + Mathf.Clamp01(height01 - position.y + 0.5f);
    }

    public TerrainGenerator(Material terrainMat)
    {
        sharedMaterial = terrainMat;

        _mcubes = new MarchingCubes();
        _mcubes.sampleProc = fSample;
        _mcubes.interpolate = true;
    }

    public GameObject GenerateChunk(Vector3 origin, int size, float voxelSize, bool active = false)
    {
        _mcubes.Reset();

        _mcubes.MarchChunk(origin, size, voxelSize);

        var mesh = new Mesh();
        mesh.vertices = _mcubes.GetVertices();
        mesh.triangles = _mcubes.GetIndices();
        mesh.uv = new Vector2[mesh.vertices.Length];
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        // true because we don't ever need to access vertices on CPU side
        // (frees up some RAM)
        mesh.UploadMeshData(true); 

        var go = new GameObject("TerrainChunk");
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mf.sharedMesh = mesh;
        mr.sharedMaterial = sharedMaterial;

        go.SetActive(active);

        return go;
    }
}
