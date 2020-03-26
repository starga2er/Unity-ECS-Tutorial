using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Mathematics;

[AlwaysSynchronizeSystem]
public class BallVelSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {

        Entities.ForEach((BallTag ballData, ref PhysicsVelocity vel) =>
        {
            Vector2 newVel = vel.Linear.xy;
            vel.Linear.xy = newVel.normalized * ballData.Speed;
            vel.Angular.z = 0;

        }).Run();

        return default;
    }
}
