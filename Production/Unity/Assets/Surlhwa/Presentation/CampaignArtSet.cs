using UnityEngine;

namespace Surlhwa.Presentation
{
    // Presentation references only. Never contains recipes, quantities or progression.
    [CreateAssetMenu(menuName="Surlhwa/ACT 1 presentation art")]
    public sealed class CampaignArtSet : ScriptableObject
    {
        public Texture2D Bottle, Jigger, Mixing, Rocks, Cocktail, Spoon, FloatSpoon, Strainer, Ice, Grip;
        public Material[] BartenderIdle;
        public Material[] OrdinaryCoat;
        public Material[] OrdinaryBun;
    }
}
