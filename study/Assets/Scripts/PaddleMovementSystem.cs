using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[AlwaysSynchronizeSystem]
public class PaddleMovementSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        
        float deltaTime = Time.DeltaTime;
        float yBound = 4.75f;

        JobHandle myJob = Entities.ForEach((ref Translation trans, in PaddleData data) =>
        {
            trans.Value.x = math.clamp(trans.Value.x + (data.speed * data.direction * deltaTime), -yBound, yBound);
        }).Schedule(inputDeps);

        return myJob;
        
    }
}