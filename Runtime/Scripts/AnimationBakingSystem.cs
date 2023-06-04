using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace TAO.VertexAnimation
{

    [RequireMatchingQueriesForUpdate]
    public partial class AnimationBakingSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (meshes,descendants,entity) in
                     SystemAPI.Query<DynamicBuffer<AnimatedSkinnedMesh>,DynamicBuffer<Child>>().WithNone<AnimatorIsBakedTag>().WithAll<AnimatorComponent>().WithEntityAccess())
            {
               
                foreach (Child child in descendants)
                {
                    meshes.Add(new AnimatedSkinnedMesh() { entity = child.Value });
                }

                entityCommandBuffer.SetComponentEnabled<AnimatorIsBakedTag>(entity,true);
                //EntityManager.RemoveComponent < AnimatorWaitingForBaking >( wait.AnimatorEntity );
            }

            entityCommandBuffer.Playback(EntityManager);
        }
    }

}