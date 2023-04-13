using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct EffectPropertyContainer
{

    public int DamageModifier;
    public EStatusEffects StatusEffectModifier;
    public List<EStatusEffects> AdditionalStatusEffects;
    public bool lightAttack;
    public bool hitFlinch;
    public bool pierceUntargetable; 
    public AttackElement attackElement;


    public EffectPropertyContainer(int dmgMod, 
                                    EStatusEffects statEffect, 
                                    List<EStatusEffects> additionalStatEffects, 
                                    bool lightAttack, 
                                    bool hitFlinch, 
                                    bool pierceUntargetable, 
                                    AttackElement attackElement)
    {
        DamageModifier = dmgMod;
        StatusEffectModifier = statEffect;
        AdditionalStatusEffects = additionalStatEffects;
        this.lightAttack = lightAttack;
        this.hitFlinch = hitFlinch;
        this.pierceUntargetable = pierceUntargetable;
        this.attackElement = attackElement;


    }


}


public abstract class ChipEffectBlueprint : MonoBehaviour
{

    public delegate void TriggeredEffectEvent();
    public event TriggeredEffectEvent triggeredEffect;

    
    public PlayerMovement player;
    protected Transform firePoint;
    [SerializeField] public ChipSO chip;
    protected UnityEngine.GameObject ObjectSummon;
    protected int BaseDamage;
    protected EStatusEffects BaseStatusEffect;
    protected int EnergyCost;

    public UnityEngine.GameObject SummonObjectModifier = null;
    public int EnergyCostModifier = 0;
    public List<Vector2Int> RangeModifier = new List<Vector2Int>();




    public int DamageModifier = 0;
    public EStatusEffects StatusEffectModifier;
    public List<EStatusEffects> AdditionalStatusEffects = new List<EStatusEffects>();
    protected bool lightAttack;
    protected bool hitFlinch;
    protected bool pierceUntargetable;

    protected EffectPropertyContainer effectProperties = new EffectPropertyContainer(0, 
                                                                                    EStatusEffects.Default, 
                                                                                    new List<EStatusEffects>(),  
                                                                                    false, 
                                                                                    false, 
                                                                                    false, 
                                                                                    AttackElement.Normal);



    protected virtual void AdditionalAwakeEvents(){}

    void Awake()
    {
        //player = PlayerMovement.Instance;

        ObjectSummon = chip.GetObjectSummon();
        BaseStatusEffect = chip.GetStatusEffect();
        BaseDamage = chip.GetChipDamage();
        lightAttack = chip.IsLightAttack();
        hitFlinch = chip.IsHitFlinch();
        pierceUntargetable = chip.IsPierceUntargetable();
        EnergyCost = chip.EnergyCost;

        effectProperties.DamageModifier = DamageModifier;
        effectProperties.StatusEffectModifier = BaseStatusEffect;
        effectProperties.hitFlinch = hitFlinch;
        effectProperties.pierceUntargetable = pierceUntargetable;


        AdditionalAwakeEvents();

    }

    private void Start() 
    {
        if(player == null)
        {
            print("Chip: " + chip.GetChipName() + " did not have its player owner set on instantiation - automatically set player owner." 
            + " This is normally not a problem, however it is less efficient than setting the player owner on instantiation." );
            player = FindObjectOfType<PlayerMovement>();
        }

        firePoint = player.firePoint;        

        
    }

    private void OnEnable() 
    {
        
    }

    public int calcFinalDamage()
    {
        int finalDamage = (int)((BaseDamage + DamageModifier) * player.AttackMultiplier);
        return finalDamage;        
    }

    public void applyChipDamage(BStageEntity entity)
    {

        if(StatusEffectModifier == EStatusEffects.Default)
        {
            StatusEffectModifier = BaseStatusEffect;
        }

        int finalDamage = (int)((BaseDamage + effectProperties.DamageModifier) * player.AttackMultiplier);

        AttackPayload attackPayload = new AttackPayload(finalDamage,
                                                        effectProperties.lightAttack,
                                                        effectProperties.hitFlinch,
                                                        effectProperties.pierceUntargetable,
                                                        player,
                                                        effectProperties.StatusEffectModifier,
                                                        effectProperties.AdditionalStatusEffects,
                                                        chip.GetChipElement());

        entity.hurtEntity(attackPayload);


        // entity.hurtEntity(finalDamage,
        //                     effectProperties.lightAttack,
        //                     effectProperties.hitFlinch, 
        //                     player, 
        //                     effectProperties.pierceUntargetable, 
        //                     effectProperties.StatusEffectModifier,
        //                     chip.GetChipElement());

    }

    


    protected void SummonObjects()
    {
        
    }

    public abstract void Effect();

    public virtual void OnActivationEffect(BStageEntity target){}


    void OnDisable()
    {
        BaseDamage = chip.GetChipDamage();
        lightAttack = chip.IsLightAttack();
        hitFlinch = chip.IsHitFlinch();
        pierceUntargetable = chip.IsPierceUntargetable();
        DamageModifier = 0;
        StatusEffectModifier = EStatusEffects.Default;
        SummonObjectModifier = null;
        EnergyCostModifier = 0;



    }



}
