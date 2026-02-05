using UniCore.Attribute;
using UnityEngine;

namespace UniCore.Audio
{
    [CreateAssetMenu(menuName = "UniCore/Audio/Node")]
    public class AudioNode : ScriptableObject
    {
        public SubAssetsAddress subAssets;
    }
}