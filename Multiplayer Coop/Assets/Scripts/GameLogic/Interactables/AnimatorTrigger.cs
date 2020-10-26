using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script for managing animations on objects (for events)
/// </summary>
public class AnimatorTrigger : MonoBehaviour
{
    Animator animator;
    bool boolValue;

    private void Awake() {
        animator = GetComponent<Animator>();
    }

    public void ToggleBoolOnAnimator(string name) {
        boolValue = !boolValue;
        animator.SetBool(name, boolValue);
    }
}
