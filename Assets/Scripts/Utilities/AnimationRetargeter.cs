using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AnimationRetargeter : MonoBehaviour
{
    public AnimationClip inputAnimation;
    public AnimationClip outputAnimation;
    public Animator animator;
    public Transform[] recievingTransforms;
    public bool doPosition;
    public bool doRotation;
    public bool doScale;
    public int frameRate;

    [Button]
    public void BEGIN()
    {
        if (inputAnimation == null || outputAnimation == null || animator == null || !inputAnimation.isHumanMotion) return;

        int IntLength = (int)(frameRate * inputAnimation.length);
        
        for (int i = 0; i < IntLength; i++)
        {
            







        }
    }

}
