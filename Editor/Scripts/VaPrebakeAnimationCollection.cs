using UnityEngine;

namespace TAO.VertexAnimation.Editor
{
    [CreateAssetMenu(menuName = "TAO/Create prebake animation collection",fileName = "[TAO][VA] Prebake animation collection")]
    public class VaPrebakeAnimationCollection : ScriptableObject
    {
        [field: SerializeField] public AnimationClip[] prebakeCollection { get; private set; }
    }
}