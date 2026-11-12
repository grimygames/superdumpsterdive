using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

[CreateAssetMenu(fileName = "New Trash", menuName = "Trash System/Item")]
public class TrashData : ScriptableObject
{
    public enum GameItemClassification
    {
        UselessDecorative,  // Spawns a lot, never chosen as target, has no interact effects
        StandardTarget,     // Has no special side effects, CAN be chosen as a find target
        SpecialEffectOnly,  // Triggers logic (Needle/Diaper/Bag), NEVER chosen as a find target
        HybridEffectTarget  // Optional extra tier: Triggers an effect AND can be a target
    }

    public enum ResourceCategory { Aluminum, Glass, Plastic, Other, Copper, None }
    
    [Header("Basic Info")]
    public string itemName;
    public Sprite itemSprite;
    public Sprite angryRaccoonSprite;
    public GameItemClassification classification; // Explicit type assignment
    public ItemType legacyType; // Kept for your internal game modes matching legacy conditions
    public ItemType type;

    [Header("Spawn Probability Optimization")]
    [Tooltip("Lower values make it highly rare. Higher values make it spawn constantly.")]
    [Range(1, 100)] public int spawnRarityWeight = 50; 

    [Header("Physics & Feel")]
    [Range(0.5f, 10.0f)] public float size = 1.0f;     
    [Range(0.1f, 10.0f)] public float weight = 1.0f;  
    [Range(0f, 1f)] public float bounciness = 0.2f;   
    
    [Header("Logic")]
    public float timeBonus = 0f;
    
    [Header("🌟 PlayFab Economy Mappings")]
    public ResourceCategory materialCategory;
    public int resourceValueAmount = 1;

    [Header("🎵 Audio Feedback")]
    [Tooltip("Played when the item spawns and lands in the dumpster.")]
    public AudioClip dropSound;
    
    [Tooltip("Played when the item is correctly found (Target).")]
    public AudioClip foundSound;
    
    [Tooltip("Played when the item is tapped but is NOT the target.")]
    public AudioClip incorrectTapSound;
    
    [Tooltip("Played when the item is thrown out of the dumpster bounds.")]
    public AudioClip tossedOutSound;

    [Tooltip("Played when a special effect is triggered (Bag pop, Needle poke, Raccoon hiss).")]
    public AudioClip specialActionSound;
    
    [Tooltip("Played the moment the player touches/grabs the item to drag it.")]
    public AudioClip grabSound;

    public enum ItemType { Junk, Target, Needle, Diaper, Bag, WalkieTalkie, Vibrator, Raccoon, TakeoutBox, FortuneCookie }
}