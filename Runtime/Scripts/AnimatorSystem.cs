using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace TAO.VertexAnimation
{

// System to update all the animations.

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct AnimatorSystem : ISystem
    {
        //private EntityQuery m_Group;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntitiesAnimationCurveLibrary>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            EntityCommandBuffer ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            EntitiesAnimationCurveLibrary curveLibrary = SystemAPI.GetSingleton<EntitiesAnimationCurveLibrary>();

            UpdateAnimatorJob job =
                new UpdateAnimatorJob
                {
                    DeltaTime = deltaTime,
                    StartIndex = 0,
                    Ecb = ecb.AsParallelWriter(),
                    entitiesAnimationCurveLibrary = curveLibrary
                };

            job.Run();
        }
    }

    [BurstCompile]
    public partial struct UpdateAnimatorJob : IJobEntity
    {
        public float DeltaTime;
        public int StartIndex;
        public EntityCommandBuffer.ParallelWriter Ecb;
        [ReadOnly] public EntitiesAnimationCurveLibrary entitiesAnimationCurveLibrary;

        [BurstCompile]
        public void Execute(
            ref AnimatorComponent animator,
            ref AnimatorBlendStateComponent animatorBlendState, DynamicBuffer<AnimatedSkinnedMesh> skinnedMeshes)
        {

            // Get the animation lib data.
            ref VA_AnimationLibraryData animationsRef = ref animator.AnimationLibrary.Value;

            int animationIndexNextBlend = 0;
            float animationTimeNextBlend = 0.0f;
            float blendValue = 0.0f;

            if (animatorBlendState.enabled)
            {
                animatorBlendState.currentDuration += DeltaTime;

                if (animatorBlendState.currentDuration >
                    animatorBlendState.duration)
                {
                    animator.AnimationIndex = animatorBlendState.toAnimationIndex;
                    animator.AnimationIndexNext = -1;
                    animator.AnimationTime = animatorBlendState.totalDuration;

                    animatorBlendState.enabled = false;

                    for (int i = 0; i < skinnedMeshes.Length; i++)
                    {
                        BlendingAnimationDataComponent blendingAnimationDataComponent =
                            new BlendingAnimationDataComponent { Value = 0.0f };

                        Ecb.SetComponent<BlendingAnimationDataComponent>(
                            StartIndex,
                            skinnedMeshes[i].entity,
                            blendingAnimationDataComponent);

                        StartIndex++;
                        SecondAnimationDataComponent vaAnimationDataComponent2 = new SecondAnimationDataComponent();

                        vaAnimationDataComponent2.Value = new float4
                        {
                            x = 0.0f,
                            y = animatorBlendState.toAnimationIndex,
                            z = 0.0f,
                            w = animatorBlendState.toAnimationIndex
                        };

                        Ecb.SetComponent<SecondAnimationDataComponent>(
                            StartIndex,
                            skinnedMeshes[i].entity,
                            vaAnimationDataComponent2);

                        StartIndex++;
                    }

                }
                else
                {
                    float blendTime =
                        1.0f / animatorBlendState.duration * animatorBlendState.currentDuration;

                    blendValue = entitiesAnimationCurveLibrary.blob.Value.GetValueAtTime(blendTime);

                    animatorBlendState.totalDuration += DeltaTime *
                                                        animationsRef.animations[animatorBlendState.toAnimationIndex].frameTime;

                    if (animatorBlendState.totalDuration >
                        animationsRef.animations[animatorBlendState.toAnimationIndex].duration)
                    {
                        // Set time. Using the difference to smoothen out animations when looping.
                        animatorBlendState.totalDuration -=
                            animationsRef.animations[animatorBlendState.toAnimationIndex].duration;

                        //animator.animationIndexNext = vaAnimatorStateComponent.Rand.NextInt( 20 );
                    }

                    // Lerp animations.
                    // Set animation for lerp.
                    animationIndexNextBlend = animatorBlendState.toAnimationIndex;

                    // Calculate next frame time for lerp.
                    animationTimeNextBlend = animatorBlendState.totalDuration +
                                             (1.0f / animationsRef.animations[animationIndexNextBlend].maxFrames);

                    if (animationTimeNextBlend > animationsRef.animations[animationIndexNextBlend].duration)
                    {
                        // Set time. Using the difference to smooth out animations when looping.
                        animationTimeNextBlend -= animatorBlendState.totalDuration;
                    }
                }
            }

            //if ( animator.AnimationName != vaAnimatorStateComponent.CurrentAnimationName )
            //{
            //	// Set the animation index on the AnimatorComponent to play this animation.
            //	animator.AnimationIndexNext = VA_AnimationLibraryUtils.GetAnimation(ref animationsRef, vaAnimatorStateComponent.CurrentAnimationName);
            //	animator.AnimationName = vaAnimatorStateComponent.CurrentAnimationName;
            //}

            // 'Play' the actual animation.
            animator.AnimationTime += DeltaTime * animationsRef.animations[animator.AnimationIndex].frameTime;

            if (animator.AnimationTime > animationsRef.animations[animator.AnimationIndex].duration)
            {
                // Set time. Using the difference to smoothen out animations when looping.
                animator.AnimationTime -= animationsRef.animations[animator.AnimationIndex].duration;

                //animator.animationIndexNext = vaAnimatorStateComponent.Rand.NextInt( 20 );
            }

            // Lerp animations.
            // Set animation for lerp.
            int animationIndexNext = animator.AnimationIndexNext;

            if (animationIndexNext < 0)
            {
                animationIndexNext = animator.AnimationIndex;

                //animator.animationIndexNext = animationIndexNext + 1;
            }

            // Calculate next frame time for lerp.
            float animationTimeNext = animator.AnimationTime +
                                      (1.0f / animationsRef.animations[animationIndexNext].maxFrames);

            if (animationTimeNext > animationsRef.animations[animationIndexNext].duration)
            {
                // Set time. Using the difference to smooth out animations when looping.
                animationTimeNext -= animator.AnimationTime;
            }

            for (int i = 0; i < skinnedMeshes.Length; i++)
            {
                FirstAnimationDataComponent vaAnimationDataComponent = new FirstAnimationDataComponent();

                vaAnimationDataComponent.Value = new float4
                {
                    x = animator.AnimationTime,
                    y = VA_AnimationLibraryUtils.GetAnimationMapIndex(ref animationsRef, animator.AnimationIndex),
                    z = animationTimeNext,
                    w = VA_AnimationLibraryUtils.GetAnimationMapIndex(ref animationsRef, animationIndexNext)
                };

                Ecb.SetComponent<FirstAnimationDataComponent>(
                    StartIndex,
                    skinnedMeshes[i].entity,
                    vaAnimationDataComponent);

                StartIndex++;

                if (animatorBlendState.enabled)
                {
                    BlendingAnimationDataComponent blendingAnimationDataComponent =
                        new BlendingAnimationDataComponent { Value = blendValue };

                    Ecb.SetComponent<BlendingAnimationDataComponent>(
                        StartIndex,
                        skinnedMeshes[i].entity,
                        blendingAnimationDataComponent);

                    StartIndex++;
                    SecondAnimationDataComponent vaAnimationDataComponent2 = new SecondAnimationDataComponent();

                    vaAnimationDataComponent2.Value = new float4
                    {
                        x = animatorBlendState.totalDuration,
                        y = VA_AnimationLibraryUtils.GetAnimationMapIndex(
                            ref animationsRef,
                            animatorBlendState.toAnimationIndex),
                        z = animationTimeNextBlend,
                        w = VA_AnimationLibraryUtils.GetAnimationMapIndex(
                            ref animationsRef,
                            animationIndexNextBlend)
                    };

                    Ecb.SetComponent<SecondAnimationDataComponent>(
                        StartIndex,
                        skinnedMeshes[i].entity,
                        vaAnimationDataComponent2);

                    StartIndex++;
                }
            }

            if (animator.AnimationIndexNext >= 0)
            {
                animator.AnimationIndex = animationIndexNext;
                animator.AnimationIndexNext = -1;
            }
        }
    }

}