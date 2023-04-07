using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrillArmHitbox : ObjectSummonAttributes
{

    BoxCollider2D boxCollider2D;

    private void Awake() 
    {
        boxCollider2D = GetComponent<BoxCollider2D>();    
    }

    private void OnEnable() 
    {
        StartCoroutine(InitializeHitbox());
    }

    public IEnumerator InitializeHitbox()
    {
        StartCoroutine(ResetTimer());
        if(AddStatusEffect != EStatusEffects.Default)
        {
            StatusEffect = AddStatusEffect;
        }else
        {
            StatusEffect = InheritedChip.GetStatusEffect();
        }

        yield return new WaitForSecondsRealtime(0.02f);
        gameObject.SetActive(false);
    }

    IEnumerator ResetTimer()
    {
        yield return new WaitForSecondsRealtime(0.6f);
        ResetAttributesToInitialState();
    }

    private void OnCollisionEnter2D(Collision2D other) 
    {
        if(other.gameObject.tag == "Enemy" || other.gameObject.tag == "Obstacle")
        {
            print("DrillArm hit target: " + other.gameObject.name);

            if(other.gameObject.GetComponent<BStageEntity>())
            {
                BStageEntity target = other.gameObject.GetComponent<BStageEntity>();

                target.hurtEntity(
                    (int)((InheritedChip.GetChipDamage() + AddDamage)* InheritedChipPrefab.player.AttackMultiplier),
                    InheritedChip.IsLightAttack(),
                    InheritedChip.IsHitFlinch(),
                    InheritedChipPrefab.player,
                    InheritedChip.IsPierceUntargetable(),
                    StatusEffect,
                    EChipElements.Breaking);
                target.AttemptShove(1, 0);                    
            }                


        }
    }

}
