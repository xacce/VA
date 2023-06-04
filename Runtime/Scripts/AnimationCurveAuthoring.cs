using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace TAO.VertexAnimation
{

    public class AnimationCurveAuthoring : MonoBehaviour
    {
        public AnimationCurve curve;
        public int samples = 256;
    }


    public class AnimationCurveAuthoringBaker : Baker<AnimationCurveAuthoring>
    {
        public override void Bake(AnimationCurveAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            var curveLibrary = new EntitiesAnimationCurveLibrary();
            using BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var curvesBlob = ref blobBuilder.ConstructRoot<CurveBlob>();
            curvesBlob.samples = authoring.samples;
            BlobBuilderArray<float> curvesArray = blobBuilder.Allocate(ref curvesBlob.points, authoring.samples);
            float[] samplePoints = authoring.curve.GenerateCurveArray(authoring.samples);
            for (int j = 0; j < samplePoints.Length; j++)
            {
                curvesArray[j] = samplePoints[j];
            }

            curveLibrary.blob = blobBuilder.CreateBlobAssetReference<CurveBlob>(Allocator.Persistent);
            AddBlobAsset(ref curveLibrary.blob, out var _);
            AddComponent(entity, curveLibrary);
        }
    }
}