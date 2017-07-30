using System;
using UnityEngine;
using System.Collections.Generic;

public class ChunkGenerationTask
{
    public IVec3 voxelOffset;
    public bool done = false;
    public int LOD;
}

public struct ChunkKey
{
    public IVec3 offset;
    public int lodScale;

    public ChunkKey(IVec3 offset, int lodScale)
    {
        this.offset = offset;
        this.lodScale = lodScale;
    }

    public override bool Equals(object obj)
    {
      if (obj == null || obj is ChunkKey == false)
        return false;
      var other = (ChunkKey)obj;
      return other.offset.x == offset.x 
        && other.offset.y == offset.y 
        && other.offset.z == offset.z
        && other.lodScale == lodScale;
    }

    public override int GetHashCode()
    {
      int hash = 17;
      hash = hash * 23 + offset.x.GetHashCode();
      hash = hash * 23 + offset.y.GetHashCode();
      hash = hash * 23 + offset.z.GetHashCode();
      hash = hash * 23 + lodScale.GetHashCode();
      return hash;
    }

    public bool Equals(ChunkKey other)
    {
      return other.offset.x == offset.x && other.offset.y == offset.y && other.offset.z == offset.z && other.lodScale == lodScale;
    }
}

public class LoadedChunk
{
    public ChunkKey key;
    public GameObject gameObject;
    public volatile bool loaded = false;
}

public class QuadTreeLoader
{
    // determines world size
    public int _maxLOD = 9;
    public int _chunkSize = 16;
    public float _voxelSize = 1.0f;
    public QuadTreeNode _rootNode;
    public TerrainGenerator _terrainGenerator;
    public Vector3 _camPos = Vector3.zero;
    public Dictionary<ChunkKey, LoadedChunk> _loadedChunks = new Dictionary<ChunkKey, LoadedChunk>(); 

    public enum NodeType
    {
        None,
        Internal,
        Leaf
    }

    public class QuadTreeNode
    {
        public int lodScale;
        public IVec3 min;
        public readonly QuadTreeNode[] children = new QuadTreeNode[4];
        public bool dirty = false;
        public bool loaded = false;
        public bool childrenLoaded = false;
        public int childIndex;

        public LoadedChunk chunk;
    }

    public QuadTreeLoader(TerrainGenerator tgen)
    {
        _rootNode = new QuadTreeNode();
        _rootNode.lodScale = 1 << _maxLOD;
        int halfLodScale = _rootNode.lodScale / 2;
        _rootNode.min = new IVec3(-halfLodScale * _chunkSize, 0, -halfLodScale * _chunkSize);
        _rootNode.dirty = false;
        _rootNode.loaded = true;
        _rootNode.childrenLoaded = false;
        _terrainGenerator = tgen;

        var go = tgen.GenerateChunk(_rootNode.min * _voxelSize, _chunkSize, _rootNode.lodScale * _voxelSize, true);
        _rootNode.chunk = new LoadedChunk() {gameObject = go, key = new ChunkKey(_rootNode.min, _rootNode.lodScale)};
        _loadedChunks.Add(_rootNode.chunk.key, _rootNode.chunk);
    }

    public bool IsLeaf(QuadTreeNode node)
    {
        if(node.lodScale == 1)
            return true;

        float chunkSize = _chunkSize * node.lodScale * _voxelSize;
        Vector3 center = node.min * _voxelSize + new Vector3(chunkSize * 0.5f, 0.0f, chunkSize * 0.5f);
        float distToCam = (center - _camPos).magnitude;
        return distToCam > chunkSize*2.0f;
    }

    public QuadTreeNode CreateNode(IVec3 min, int size, int childIndex)
    {
        var node = new QuadTreeNode();
        node.min = min;
        node.lodScale = size;
        node.childIndex = childIndex;
        return node;
    }

    public void DestroyNodesRecursive(QuadTreeNode node)
    {
        if(node == null)
            return;
        for(int i = 0; i < 4; i++)
        {
            DestroyNodesRecursive(node.children[i]);
        }
        if(node.chunk != null)
        {
            if(node.chunk.gameObject != null)
            {
                var mf = node.chunk.gameObject.GetComponent<MeshFilter>();
                if(mf != null)
                {
                    MonoBehaviour.Destroy(mf.sharedMesh);
                }
                MonoBehaviour.Destroy(node.chunk.gameObject);
                node.chunk.gameObject = null;
            }
            _loadedChunks.Remove(node.chunk.key);
            node.chunk = null;
        }
    }

    void UpdateTree(QuadTreeNode node, bool siblingsLoaded)
    {
        if(node == null)
            return;

        bool isLeaf = IsLeaf(node);
        node.loaded = node.chunk != null && node.chunk.loaded;
        node.childrenLoaded = false;
        // node has children, update them
        if(node.children[0] != null)
        {
            node.childrenLoaded = true;
            for(int i = 0; i < 4; i++)
            {
                node.childrenLoaded &= node.children[i] != null && node.children[i].chunk != null && node.children[i].chunk.loaded;
            }
            for(int i = 0; i < 4; i++)
            {
                UpdateTree(node.children[i], node.childrenLoaded);
            }
        }
        // node doesn't have children but should have (but don't create children if siblings not loaded yet)
        else if(!isLeaf && siblingsLoaded && node.children[0] == null)
        {
            var min = node.min;
            int halfLodScale = node.lodScale / 2;
            int chunkSize = halfLodScale * _chunkSize;
            node.children[0] = CreateNode(min, halfLodScale, 0);
            node.children[1] = CreateNode(min + new IVec3(chunkSize, 0, 0), halfLodScale, 1);
            node.children[2] = CreateNode(min + new IVec3(0, 0, chunkSize), halfLodScale, 2);
            node.children[3] = CreateNode(min + new IVec3(chunkSize, 0, chunkSize), halfLodScale, 3);
        }

        if(isLeaf || !node.childrenLoaded)
        {
            LoadedChunk lc;
            ChunkKey ck = new ChunkKey(node.min, node.lodScale);
            if(!_loadedChunks.TryGetValue(ck, out lc))
            {
                lc = new LoadedChunk();
                lc.key = ck;
                if(node.lodScale == 1 || true)
                {
                    lc.gameObject = _terrainGenerator.GenerateChunk(node.min * _voxelSize, _chunkSize, _voxelSize * node.lodScale, false);
                }
                lc.loaded = true;
                node.chunk = lc;
                _loadedChunks.Add(ck, lc);
            }
            if(node.chunk.gameObject != null)
                node.chunk.gameObject.SetActive(true);
        }
        else if(!isLeaf && node.childrenLoaded)
        {
            // TODO: swap to children
            if(node.chunk.gameObject != null)
                node.chunk.gameObject.SetActive(false);
        }
        if(isLeaf && node.loaded)
        {
            // TODO: destroy children
            for(int i = 0; i < 4; i++)
            {
                DestroyNodesRecursive(node.children[i]);
                node.children[i] = null;
            }
        }

        // reload if dirty
        if(node.dirty && node.loaded)
        {
            // TODO: reload
            node.dirty = false;
        }
    }

    public void Update(Vector3 camPos)
    {
        _camPos = camPos;

        UpdateTree(_rootNode, true);
    }
}

