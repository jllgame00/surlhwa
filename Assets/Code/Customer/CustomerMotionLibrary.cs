using System;
using UnityEngine;

/// <summary>Art only. A missing entry never changes customer progression.</summary>
public sealed class CustomerMotionLibrary : ScriptableObject
{
    [Serializable]
    public sealed class Poses
    {
        public string character;
        public Sprite[] walk;
        public Sprite turn, seated, listening, reaction;
    }
    public Poses[] characters;
    public Poses Find(string character) => Array.Find(characters, p => p.character == character);
}
