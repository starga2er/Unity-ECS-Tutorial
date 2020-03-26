using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Mathematics;

[AlwaysSynchronizeSystem]
public class PlayerInputSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {

        Entities.ForEach((ref PaddleData moveData) =>
        {
            moveData.direction = 0;

            moveData.direction -= Input.GetKey(moveData.leftKey) ? 1 : 0;
            moveData.direction += Input.GetKey(moveData.rightKey) ? 1 : 0;
        }).Run();
        
        return default;
    }
}
