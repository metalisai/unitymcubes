using System;
using UnityEngine;

public class GameWorld : MonoBehaviour
{
    public Material terrainMaterial;

    QuadTreeLoader _worldLoader;
    TerrainGenerator _terrainGenerator;

    private void Start()
    {
        _terrainGenerator = new TerrainGenerator(terrainMaterial);
        _worldLoader = new QuadTreeLoader(_terrainGenerator);
    }

    private void Update()
    {
        _worldLoader.Update(transform.position);
    }
}
