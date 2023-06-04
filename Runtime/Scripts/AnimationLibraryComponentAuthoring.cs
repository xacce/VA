using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;
using Random = Unity.Mathematics.Random;

namespace TAO.VertexAnimation
{
    [UnityEngine.DisallowMultipleComponent]
    public class AnimationLibraryComponentAuthoring : UnityEngine.MonoBehaviour
    {
        public AnimationLibrary AnimationLibrary;
        public bool DebugMode = false;
        public uint Seed;
    }

    public partial struct AnimatorIsBakedTag : IComponentData, IEnableableComponent
    {
    }

    public partial struct AnimationLibraryComponent : IComponentData
    {
        public BlobAssetReference<VA_AnimationLibraryData> AnimLibAssetRef;
        // public BlobAssetStore BlobAssetStore;
    }

    public class AnimationLibraryComponentBaker : Baker<AnimationLibraryComponentAuthoring>
    {
        public override void Bake(AnimationLibraryComponentAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic); //TODO idk dynamic or none, need checks
            authoring.AnimationLibrary.Init();
            AnimationLibraryComponent animationLibrary = new AnimationLibraryComponent();
            using (BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp))
            {
                // Construct the root.
                ref VA_AnimationLibraryData animationDataBlobAsset = ref blobBuilder.ConstructRoot<VA_AnimationLibraryData>();

                // Set all the data.
                BlobBuilderArray<VA_AnimationData> animationDataArray = blobBuilder.Allocate(ref animationDataBlobAsset.animations, authoring.AnimationLibrary.animationData.Count);

                for (int i = 0; i < animationDataArray.Length; i++)
                {
                    // Copy data.
                    animationDataArray[i] = authoring.AnimationLibrary.animationData[i];

                    if (authoring.DebugMode)
                    {
                        UnityEngine.Debug.Log("VA_AnimationLibrary added " + animationDataArray[i].name.ToString());
                    }
                }

                // Construct blob asset reference.
                //BlobAssetReference<VA_AnimationLibraryData> animLibAssetRef = blobBuilder.CreateBlobAssetReference<VA_AnimationLibraryData>(Allocator.Persistent);
                // Static because of multi scene setup.
                animationLibrary.AnimLibAssetRef = blobBuilder.CreateBlobAssetReference<VA_AnimationLibraryData>(Allocator.Persistent);
                AddBlobAsset(ref animationLibrary.AnimLibAssetRef, out var hash128);

                if (authoring.DebugMode)
                {
                    UnityEngine.Debug.Log("VA_AnimationLibrary has " + animationLibrary.AnimLibAssetRef.Value.animations.Length.ToString() + " animations.");
                }
            }
            AddComponent(entity, animationLibrary);

            BlobAssetReference<VA_AnimationLibraryData> animLib = animationLibrary.AnimLibAssetRef;
            // Get the animation lib data.
            Random random = new Random(authoring.Seed != 0 ? authoring.Seed : 42);
            int index = random.NextInt(20);

            // Add animator to 'parent'.
            AnimatorComponent animatorComponent = new AnimatorComponent
            {
                AnimationName = "Idk this",
                AnimationIndex = 0,
                AnimationIndexNext = -1,
                AnimationTime = 0,
                AnimationLibrary = animLib

            };
            AddComponent(entity, animatorComponent);

            AnimatorBlendStateComponent animatorStateComponent = new AnimatorBlendStateComponent
            {
                enabled = false,
                toAnimationIndex = 1,
                currentDuration = 0.0f,
                blendingCurveIndex = 0,
                duration = 2.5f,
                totalDuration = 0.0f,
            };

            AddBuffer<AnimatedSkinnedMesh>(entity);
            AddComponent(entity, animatorStateComponent);
            AddComponent<AnimatorIsBakedTag>(entity);
            SetComponentEnabled<AnimatorIsBakedTag>(entity, false);
        }
    }


    public partial struct AnimatorComponent : IComponentData
    {
        public FixedString64Bytes AnimationName;
        public int AnimationIndex;
        public int AnimationIndexNext;
        public float AnimationTime;

        public BlobAssetReference<VA_AnimationLibraryData> AnimationLibrary;
        // public NativeArray<Entity> SkinnedMeshes;
    }

    public partial struct AnimatedSkinnedMesh : IBufferElementData
    {
        public Entity entity;
    }

    public partial struct AnimatorBlendStateComponent : IComponentData
    {
        public bool enabled;
        public float totalDuration;
        public int toAnimationIndex;
        public float duration;
        public float currentDuration;
        public int blendingCurveIndex;

        public void To(int animationIndex)
        {
            enabled = true;
            toAnimationIndex = animationIndex;
            duration = 0.4f;
            blendingCurveIndex = 0;
            currentDuration = 0;
        }
    }

}