using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerChipAnimations : MonoBehaviour
{
 

    Dictionary<int, string> animationDictionary = new Dictionary<int, string>();
    float currentAnimationLength;
    private string currentState;
    int currentAnimationID;
    bool isInAnimation = false;

    Animation currentAnimation;

    private void Awake() {
        animationDictionary.Add(0, "MegamanCannon");
        animationDictionary.Add(1, "MegamanSword");
        animationDictionary.Add(2,"ShieldActivate");
        animationDictionary.Add(3, "Vulcan");
        animationDictionary.Add(4, "AreaGrab");
        
    }

    Animator myAnimator;

    private void Start() {
        myAnimator = GetComponent<Animator>();
    }

    public void playAnimationID(int id, float duration)
    {
        ChangeAnimationState(animationDictionary[id]);
        StartCoroutine(ReturnToIdle(duration));
        Debug.Log(duration.ToString() + " Animation Played:" + animationDictionary[currentAnimationID]);
    }

    public void playAnimationEnum(EChips chipAnim, float duration)
    {
        ChangeAnimationState(Enum.GetName(typeof(EMegamanAnimations), chipAnim));
        print("Animation played: " + Enum.GetName(typeof(EMegamanAnimations), chipAnim));
        StartCoroutine(ReturnToIdle(duration));

    }

    IEnumerator ReturnToIdle(float duration)
    {
        yield return new WaitForSeconds(duration);
        ChangeAnimationState("Megaman_Idle");
    }

    void ChangeAnimationState(string newState)
    {
        if(currentState == newState) return;
        myAnimator.Play(newState);
        currentState = newState;
    }


}