using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AnyStateAnimator : MonoBehaviour
{
    private Animator animator;
    private Dictionary<string, AnyStateAnimation> anyStateAnimatioins = new Dictionary<string, AnyStateAnimation>();

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        Animate();
    }

    public void TryPlayAnimaiton(string animationName)
    {
        if (!anyStateAnimatioins.ContainsKey(animationName)) return;
        
        // If it's already the active playing animation, don't do anything
        if (anyStateAnimatioins[animationName].IsPlaying) return;

        bool startAnimation = true;
        if (anyStateAnimatioins[animationName].HigherPrio != null)
        {
            foreach (string animName in anyStateAnimatioins[animationName].HigherPrio)
            {
                if (anyStateAnimatioins[animName].IsPlaying)
                {
                    startAnimation = false;
                    break;
                }
            }
        }

        if (startAnimation)
        {
            StartAnimation(animationName);
        }
    }

    private void StartAnimation(string animationName)
    {
        // Set EVERY animation to false first
        foreach (string animName in anyStateAnimatioins.Keys.ToList())
        {
            anyStateAnimatioins[animName].IsPlaying = false;
            animator.SetBool(animName, false); // Force sync with Animator
        }
        
        // Set the target animation to true
        anyStateAnimatioins[animationName].IsPlaying = true;
        animator.SetBool(animationName, true);
    }

    public void AddAnimation(params AnyStateAnimation[] animations)
    {
        foreach (var anim in animations)
        {
            if(!anyStateAnimatioins.ContainsKey(anim.AnimationName))
                anyStateAnimatioins.Add(anim.AnimationName, anim);
        }
    }

    private void Animate()
    {
        foreach (string key in anyStateAnimatioins.Keys)
        {
            animator.SetBool(key, anyStateAnimatioins[key].IsPlaying);
        }
    }

    public void OnAnimationDone(string animationName)
    {
        if (anyStateAnimatioins.ContainsKey(animationName))
        {
            anyStateAnimatioins[animationName].IsPlaying = false;
            animator.SetBool(animationName, false);
        }
    }
}