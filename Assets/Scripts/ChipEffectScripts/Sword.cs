using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sword : MonoBehaviour, IChip
{
    public Transform firePoint;
    public int BaseDamage {get;set;} = 80;

    public int AdditionalDamage{get; set;} = 0;

    public EChipTypes ChipType => EChipTypes.Active;
    public EStatusEffects statusEffect {get;set;} = EStatusEffects.Default;
    public EChipElements chipElement => EChipElements.Blade;



    PlayerMovement player;


    public void Effect(int AddDamage = 0, EStatusEffects statusEffect = EStatusEffects.Default)
    {

        AdditionalDamage += AddDamage;

        firePoint = FindObjectOfType<ChipEffects>().firePoint;
        player = GetComponent<PlayerMovement>();

        RaycastHit2D hitInfo = Physics2D.Raycast (firePoint.position, firePoint.right, 1f, LayerMask.GetMask("Enemies"));
        if(hitInfo)
        {
          
          IBattleStageEntity script = hitInfo.transform.gameObject.GetComponent<IBattleStageEntity>();
          script.hurtEntity((int)((BaseDamage + AdditionalDamage) * player.AttackMultiplier), false, true);
          Debug.Log(hitInfo.transform.name + "HP:" + script.getHealth() + "Distance: " + hitInfo.distance.ToString() + "Chip Used: Sword");
        }

      AdditionalDamage = 0;

    }
}