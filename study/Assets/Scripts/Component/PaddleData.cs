using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct PaddleData : IComponentData
{
    public KeyCode leftKey;
    public KeyCode rightKey;
    public float speed;
    public float direction;
}
