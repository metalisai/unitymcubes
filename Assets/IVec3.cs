using System;
using UnityEngine;

public struct IVec3
{
    public IVec3(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static IVec3 operator +(IVec3 a, IVec3 b)
    {
        return new IVec3(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public static IVec3 operator -(IVec3 a, IVec3 b)
    {
        return new IVec3(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public static Vector3 operator *(float multiplier, IVec3 v)
    {
        return new Vector3(multiplier * v.x, multiplier * v.y, multiplier * v.z);
    }

    public static Vector3 operator *(IVec3 v, float multiplier)
    {
        return new Vector3(multiplier * v.x, multiplier * v.y, multiplier * v.z);
    }

    public int x,y,z;
}
