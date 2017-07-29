using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    MarchingCubes _mcubes;
    const float voxelSize = 1.0f;
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

    void Start()
    {
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        sharedMaterial = new Material(Shader.Find("Standard"));

        _mcubes = new MarchingCubes();
        _mcubes.sampleProc = fSample;
        //_mcubes.interpolate = true;

        int chunkSize = 10;
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                GenerateChunk(new Vector3(i*voxelSize*chunkSize, 0.0f, j*voxelSize*chunkSize), chunkSize);
            }
        }

        sw.Stop();
        Debug.LogFormat("Generation took {0} seconds", sw.Elapsed.TotalSeconds);
    }

    GameObject GenerateChunk(Vector3 origin, int size)
    {
        _mcubes.Reset();

        /*// about 3x slower
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
        }*/

        
        _mcubes.MarchChunk(origin, size, voxelSize);

        var mesh = new Mesh();
        mesh.vertices = _mcubes.GetVertices();
        mesh.triangles = _mcubes.GetIndices();
        mesh.uv = new Vector2[mesh.vertices.Length];
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        var go = new GameObject("TerrainChunk");
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mf.sharedMesh = mesh;
        mr.sharedMaterial = sharedMaterial;
        return go;
    }
}
