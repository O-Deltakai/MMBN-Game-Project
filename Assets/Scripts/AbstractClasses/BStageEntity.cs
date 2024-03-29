using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using DG.Tweening;
using FMODUnity;

///<summary>
///A container data type which stores information on the properties of an incoming attack.
///All variables within the attack payload may be modified freely. 
///</summary>
public struct AttackPayload
{
    public int damage;
    public bool lightAttack;
    public bool hitFlinch;
    public bool pierceUntargetable;
    public BStageEntity attacker;
    public EStatusEffects statusEffect;
    public List<EStatusEffects> additionalStatusEffects;
    public AttackElement attackElement;

    public AttackPayload(int damage,
                         bool lightAttack,
                          bool hitFlinch, 
                          bool pierceUntargetable, 
                          BStageEntity attacker, 
                          EStatusEffects statusEffect, 
                          List<EStatusEffects> additionalStatusEffects,  
                          AttackElement attackElement)
    {
        this.damage = damage;
        this.lightAttack = lightAttack;
        this.hitFlinch = hitFlinch;
        this.pierceUntargetable = pierceUntargetable;
        this.attacker = attacker;
        this.statusEffect = statusEffect;
        this.additionalStatusEffects = additionalStatusEffects;
        this.attackElement = attackElement;

    }

}


[RequireComponent(typeof(StatusEffectManager))]
public abstract class BStageEntity : MonoBehaviour
{

#region Events and Delegates

    public delegate void OnDeathEvent(BStageEntity entity);
    public virtual event OnDeathEvent DeathEvent;

    public delegate void OnHurtEvent(BStageEntity entity);
    public virtual event OnHurtEvent hurtEvent;

    public delegate void OnParryEvent(BStageEntity entity);
    public virtual event OnParryEvent parryEvent;


    public delegate void MoveOntoTileEvent(int x, int y, BStageEntity entity);
    public event MoveOntoTileEvent moveOntoTile;
    public delegate void MoveOffTileEvent(int x, int y, BStageEntity entity);
    public event MoveOffTileEvent moveOffTile;


#endregion

#region Initialized Variables and Classes

    protected static TileEventManager tileEventManager;
    protected static BattleStageHandler stageHandler;
    protected StatusEffectManager statusManager;
    [HideInInspector]
    public SpriteRenderer spriteRenderer;
    protected static Shader shaderGUItext;
    protected static Shader shaderSpritesDefault;
    protected Animator animator;
    [SerializeField] public TextMeshProUGUI healthText;

    public float objectTimeScale = 1f;

    ///<summary>
    ///worldTransform is the world position and transform of the BStageEntity.
    ///The components which make up the entity normally fall under this worldTransform
    ///as children. Use this when you need to set the cell position of the object in relation
    ///to the stage grid. Do not use this if you need to set children object sprite positions.
    ///</summary>
    [HideInInspector] public Transform worldTransform;

    ///<summary>
    ///Determines if the entity can be affected by tile effects
    ///</summary>
    public abstract bool isGrounded{get;set;}

    ///<summary>
    ///Determines if the entity can be moved from its initial position and cannot move from its position.
    ///</summary>
    public abstract bool isStationary{get;}
    public abstract bool isStunnable{get;}
    public abstract int maxHP{get;}
    public abstract ETileTeam team{get; set;}

    ///<summary>
    ///If an entity is an obstacle it will block most chip effects from passing through them.
    ///E.g. An attack which should pass through multiple entities or spawn on top of the entity
    ///will be blocked by this entity. This property should only be true on structure-based
    ///entities like solid blocks and is not meant for intelligent entities.
    ///</summary>
    public virtual bool isObstacle{get;} = false;
    public bool isBeingShoved = false;
    public bool MarkedForDeath = false;
    protected virtual bool isPlayer{get;} = false;

    public bool isRooted = false;
    [HideInInspector] protected bool isMoving = false;

    ///<summary>
    ///A non-volatile status effect is a status which cannot be overriden
    ///by another non-volatile status until the end of its duration.
    ///</summary>
    [HideInInspector] public bool nonVolatileStatus = false;
    
    ///<summary>
    ///This is a calculated variable that is meant to define the current cell
    ///position of the entity on the battle stage grid. It is calculated by
    ///checking the worldTransform and performing a conversion function on its position
    ///to a Vector3Int which will match its derived position on the battle stage grid.
    ///</summary>
    public Vector3Int currentCellPos;
    public Vector3Int previousCellPos;
    ///<summary>
    ///The list of tiles on the battle stage grid which are claimed by this entity.
    ///See the ClaimTileOccupancy method for additional details.
    ///</summary>
    public List<StageTile> claimedTiles = new List<StageTile>();
    
    [SerializeField] public int currentHP;
    [SerializeField] public int shieldHP;
    [SerializeField] public double DefenseMultiplier = 1;
    [SerializeField] public double AttackMultiplier = 1;
    public float stunResistance = 1;  
    [SerializeField] public bool isUntargetable = false;
    [SerializeField] public bool fullInvincible = false;
    [SerializeField] public bool isStunned = false;


    protected Color invisible;
    protected Color opaque;
    public Color defaultColor;

    protected Coroutine AnimateHPCoroutine;
    RectTransform DefaultHPNumberPosition;
    protected Coroutine isMovingCoroutine;
    protected Coroutine EntityDestructionCoroutine;


    [SerializeField] protected UnityEngine.GameObject destructionVFX;

#endregion

[Header("Generic Sound Events")]
    [SerializeField] public EventReference HurtSFX;
    [SerializeField] public EventReference DestroyedSFX;


    void Reset()
    {

    }

    protected void InitializeAwakeVariables()
    {
        tileEventManager = FindObjectOfType<TileEventManager>();
        stageHandler = FindObjectOfType<BattleStageHandler>();
        worldTransform = transform.parent.transform;
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        statusManager = GetComponent<StatusEffectManager>();
        shaderGUItext = Shader.Find("GUI/Text Shader");
        shaderSpritesDefault = shaderSpritesDefault = Shader.Find("Sprites/Default");

        invisible = spriteRenderer.color;
        opaque = spriteRenderer.color;
        defaultColor = spriteRenderer.color;
        invisible.a = 0;
        opaque.a = 1;

        DefaultHPNumberPosition = (RectTransform)healthText.gameObject.transform.parent.transform;

        hurtEvent += CheckFinalHPForDeath;

    }

    public void SetUntargetable(bool condition)
    {
        if(condition)
        {
            isUntargetable = true;
        }else
        {
            isUntargetable = false;
        }
    }

    public void SetInvincible(bool condition)
    {
       if(condition)
        {
            fullInvincible = true;
        }else
        {
            fullInvincible = false;
        }
    }

    public void ToggleInvincible()
    {
        if(fullInvincible)
        {
            fullInvincible = false;
        }else
        {
            fullInvincible = true;
        }
    }

    public void ToggleUntargetable()
    {
        if(isUntargetable)
        {
            isUntargetable = false;
        }else
        {
            isUntargetable = true;
        }        
    }

    public virtual void Awake()
    {
        InitializeAwakeVariables();


    }


    public virtual void Start()
    {
        currentCellPos.Set((int)(Math.Round((worldTransform.position.x/1.6f), MidpointRounding.AwayFromZero)),
                            (int)transform.parent.position.y, 0);
        stageHandler.setCellOccupied(currentCellPos.x, currentCellPos.y, true);
        healthText.text = currentHP.ToString();

    }


    public Vector3Int CalculateCurrentCellPosition(Transform transform)
    {
        Vector3Int calculatedCellPos = new Vector3Int ((int)(Math.Round((transform.position.x/1.6f), MidpointRounding.AwayFromZero)),
                            (int)transform.parent.position.y, 0);

        return calculatedCellPos;
    }

    protected bool isAnimatingHP = false;
    ///<summary>
    ///<para>
    ///lightAttack dictates whether the attack triggers invincibility frames.
    ///</para>
    ///<para>
    ///hitFlinch dictates whether the attack will trigger a flinch animation (if it is able to) on the target.
    ///</para>
    ///pierceUntargetable dictates whether the attack can hurt the target even if they are set to isUntargetable.
    ///</summary>
    public virtual void hurtEntity(int damage,
                                   bool lightAttack,
                                   bool hitFlinch,
                                   BStageEntity attacker = null,
                                   bool pierceUntargetable = false,
                                   EStatusEffects statusEffect = EStatusEffects.Default,
                                   AttackElement attackElement = AttackElement.Normal                                   
                                   )
    {
        if(fullInvincible)
        {return;}
        if(isUntargetable && !pierceUntargetable)
        {return;}   

        if(statusEffect != EStatusEffects.Default)
        {StartCoroutine(setStatusEffect(statusEffect, 1));}


        //Animate HP block
        if(damage >= 10)
        {
            isAnimatingHP = true;

            if(AnimateHPCoroutine != null)
            {
                StopCoroutine(AnimateHPCoroutine);
            }

            AnimateHPCoroutine = StartCoroutine(animateNumber(currentHP, currentHP - Mathf.Clamp((int)(damage * DefenseMultiplier), 1, 999999)));
        }


        RuntimeManager.PlayOneShotAttached(HurtSFX, this.gameObject);

        //Calculate new current HP value after taking damage
        currentHP = Mathf.Clamp(currentHP - Mathf.Clamp((int)(damage * DefenseMultiplier), 1, 999999), 0, currentHP);
        AnimateShakeNumber(damage);
       

        if(AnimateHPCoroutine == null)
        {
            healthText.text = currentHP.ToString();
        }

        if(hurtEvent != null)
        {
            hurtEvent(this);
        } 

        StartCoroutine(DamageFlash());

        return; 
    }

    public virtual void hurtEntity(AttackPayload payload)
    {
        if(fullInvincible)
        {return;}
        if(isUntargetable && !payload.pierceUntargetable)
        {return;}


        AttackPayload attackPayload = payload; 
        
        if(MarkedForDeath)
        {
           
           attackPayload = TriggerMarkEffect(payload.attackElement, payload);
           MarkedForDeath = false;
           statusManager.triggerStatusEffect(EStatusEffects.MarkForDeath, 0, false);
        }

        if(attackPayload.statusEffect != EStatusEffects.Default)
        {
            
            statusManager.triggerStatusEffect(payload.statusEffect);
            //StartCoroutine(setStatusEffect(attackPayload.statusEffect, 1));
        }else
        {
            print("Chip payload used default status effect");
        }

        if(payload.additionalStatusEffects.Count == 0)
        {
            print("additional status effects is empty");
        }
        foreach(EStatusEffects statusEffect in payload.additionalStatusEffects)
        {
            print("Additional status effects: " + statusEffect);
            statusManager.triggerStatusEffect(statusEffect);
            //StartCoroutine(setStatusEffect(statusEffect, 1));

        }

        if(attackPayload.damage >= 10)
        {
            isAnimatingHP = true;

            if(AnimateHPCoroutine != null)
            {
                StopCoroutine(AnimateHPCoroutine);
            }

            AnimateHPCoroutine = StartCoroutine(animateNumber(currentHP, currentHP - Mathf.Clamp((int)(attackPayload.damage * DefenseMultiplier), 1, 999999)));
        }

        RuntimeManager.PlayOneShotAttached(HurtSFX, this.gameObject);

        //Calculate new current HP value after taking damage
        currentHP = Mathf.Clamp(currentHP - Mathf.Clamp((int)(attackPayload.damage * DefenseMultiplier), 1, 999999), 0, currentHP);
        AnimateShakeNumber(attackPayload.damage);
       

        if(AnimateHPCoroutine == null)
        {
            healthText.text = currentHP.ToString();
        }


        if(hurtEvent != null)
        {
            hurtEvent(this);
        } 

        StartCoroutine(DamageFlash());

    }


    List<Coroutine> DoTList = new List<Coroutine>();    

    ///<summary>
    ///Different from HurtEntity; this one
    ///subtracts HP directly from the target, bypassing any form of resistances. Can be
    ///given a tickrate and duration for damage over time effects.
    ///</summary>
    public void DamageEntity(int damage, float tickrate = float.Epsilon, float duration = float.Epsilon)
    {
        if(tickrate > float.Epsilon && duration > float.Epsilon)
        {
            StartCoroutine(DamageOverTime(damage: damage, tickrate: tickrate, duration: duration));
            
            
        }else
        {
            if(damage >= currentHP)
            {
                RuntimeManager.PlayOneShotAttached(HurtSFX, this.gameObject);
                AnimateShakeNumber(damage);
                StartCoroutine(DestroyEntity());                
                return;
            }

            AnimateShakeNumber(damage);
            if(damage >= 5)
            {
                isAnimatingHP = true;

                if(AnimateHPCoroutine != null)
                {
                    StopCoroutine(AnimateHPCoroutine);
                }

                AnimateHPCoroutine = StartCoroutine(animateNumber(currentHP, currentHP - Mathf.Clamp((int)(damage), 1, 999999)));
            }            
            currentHP -= damage;
        }

    }

    ///<summary>
    ///Applies a damage over time effect on this entity, reducing their health by the given
    ///damage with a given tickrate over a given duration. E.g. 10 damage with 0.5 tickrate over 10
    ///seconds will deal 200 damage in total.
    ///</summary>
    IEnumerator DamageOverTime(int damage, float tickrate, float duration)
    {
        float damageDuration = duration;
        WaitForSeconds damageTick = new WaitForSeconds(tickrate);

        while(damageDuration > 0)
        {


            if(damage >= 5)
            {
                isAnimatingHP = true;

                if(AnimateHPCoroutine != null)
                {
                    StopCoroutine(AnimateHPCoroutine);
                }

                AnimateHPCoroutine = StartCoroutine(animateNumber(currentHP, currentHP - Mathf.Clamp((int)(damage), 1, 999999)));
            } 

            if(damage >= currentHP)
            {
                RuntimeManager.PlayOneShotAttached(HurtSFX, this.gameObject);
                AnimateShakeNumber(damage);
                StartCoroutine(DestroyEntity());                
                break;
            }

            currentHP -= damage;

            yield return damageTick;

            damageDuration -= tickrate;
            

        }


        yield return null;
    }        

    public AttackPayload TriggerMarkEffect(AttackElement chipElement, AttackPayload payload)
    {
        AttackPayload modifiedPayload = payload;
        print("Attempted to trigger mark effect wtih element: " + chipElement);

        switch (chipElement) {
            case AttackElement.Normal:
                modifiedPayload.damage = modifiedPayload.damage * 2;
                print("Triggered normal element mark effect");

                break;

            case AttackElement.Air:
                print("Triggered air element mark effect");
                break;

            case AttackElement.Blade:
                modifiedPayload.additionalStatusEffects.Add(EStatusEffects.Bleeding);
                print("Triggered blade element mark effect");

                break;

            case AttackElement.Fire:
                modifiedPayload.additionalStatusEffects.Add(EStatusEffects.Burning);
                print("Triggered fire element mark effect");
                break;

            case AttackElement.Water:
                modifiedPayload.additionalStatusEffects.Add(EStatusEffects.Frozen);
                print("Triggered water element mark effect");
                break;

            case AttackElement.Electric:
                modifiedPayload.additionalStatusEffects.Add(EStatusEffects.Paralyzed);
                print("Triggered electric element mark effect");
                break;

            case AttackElement.Grass:
                print("Triggered grass element mark effect");
                break;

            case AttackElement.Breaking:
                print("Triggered breaking element mark effect");
                break;

            default:
            Debug.LogWarning("Chip element: " + chipElement + " does not have a mark effect implemented. Aborted triggering mark effect.");
            break;
        }

        return modifiedPayload;
    }

    protected void CheckFinalHPForDeath(BStageEntity entity)
    {
        if(currentHP <= 0)
        {
            StartCoroutine(DestroyEntity());   
        }
    }

    public int getHealth()
    {
        return currentHP;
    }

    public IEnumerator DamageFlash()
    {
        if(currentHP <= 0)
        {yield break;}
        setSolidColor(Color.white);
        yield return new WaitForSecondsRealtime(0.03f);
        setNormalSprite();
    }

    public virtual IEnumerator DestroyEntity()
    {
        AdditionalDestructionEvents();
        animator.speed = Mathf.Epsilon;
        currentHP = 0;
        healthText.text = currentHP.ToString();
            
        if(AnimateHPCoroutine == null)
        {
            healthText.enabled = false;
        }
        fullInvincible = true;

        yield return new WaitForSecondsRealtime(0.0005f);
        FMODUnity.RuntimeManager.PlayOneShotAttached(DestroyedSFX, this.gameObject);
        setSolidColor(Color.white);
        Instantiate(destructionVFX, transform.parent.transform.position, 
                    transform.rotation, transform.parent.transform);

        // var vfx = Addressables.InstantiateAsync("VFX_Destruction_Explosion", transform.parent.transform.position, 
        //                                         transform.rotation, transform.parent.transform);
        yield return new WaitForSecondsRealtime(0.4f);

        if(AnimateHPCoroutine == null)
        {
            healthText.enabled = false;
        }        

        if(DeathEvent != null)
        {
            DeathEvent(this);
        }

        stageHandler.setCellEntity(currentCellPos.x, currentCellPos.y, this, false);
        ClearClaimedTiles();
        tileEventManager.UnsubscribeEntity(this);
        //yield return new WaitForSecondsRealtime(0.1f);
        Destroy(transform.parent.gameObject);
        Destroy(gameObject);

    }

    ///<summary>
    ///Empty virtual method to allow for a specific entity to initalize events additional to the default DestroyEntity method
    ///present on all BStageEntities. These additional events will always run first before the rest of the
    ///DestroyEntity method is run.
    ///</summary>
    public virtual void AdditionalDestructionEvents(){}

    public void teleportToCell(int x, int y)
    {
            stageHandler.setCellEntity(currentCellPos.x, currentCellPos.y, this, false);
            stageHandler.SetPreviousSeenEntity(currentCellPos.x, currentCellPos.y, this, true);
            previousCellPos.Set(currentCellPos.x, currentCellPos.y, 0);

            currentCellPos.Set(x, y, 0);

            stageHandler.setCellEntity(currentCellPos.x, currentCellPos.y, this, true);

            worldTransform.localPosition = stageHandler.stageTilemap.
                                        GetCellCenterWorld(currentCellPos);
    }

    public void setSolidColor(Color color)
    {
        spriteRenderer.material.shader = shaderGUItext;
        spriteRenderer.color = color;
    }
    public void setNormalSprite()
    {
        spriteRenderer.material.shader = shaderSpritesDefault;
        spriteRenderer.color = Color.white;
    }

    ///<summary>
    ///Checks if the tile at the exact x and y position given is valid.
    ///Returns true if valid, false otherwise.
    ///</summary>
    public bool checkValidTile(int x, int y)
    {
        Vector3Int coordToCheck = new Vector3Int(x, y, 0);
        StageTile stageTileToCheck = null;        

        //Checks if the tile exists within the stageTiles dictionary (i.e it is not empty space), else return false.
        if(stageHandler.stageTiles.ContainsKey(stageHandler.stageTilemap.CellToWorld(coordToCheck)))
        {
            stageTileToCheck = stageHandler.stageTiles
                [stageHandler.stageTilemap.CellToWorld(coordToCheck)];
        }else
        {
            return false;
        }

        CustomTile customTileToCheck = stageHandler.getCustTile(coordToCheck);

        if(stageTileToCheck.entity == this)
        {
            return true;
        }
        
        if(stageHandler.stageTilemap.GetTile
            (coordToCheck) == null
                ||
            customTileToCheck.GetTileTeam() != team
                ||
            stageTileToCheck.isOccupied
                ||
            (isGrounded && !customTileToCheck.isPassable)
                ||
            (stageTileToCheck.entityClaimant != null && stageTileToCheck.entityClaimant != this)
                ||
            (stageTileToCheck.entity != null && stageTileToCheck.entity !=this)
            )
            {
                return false;
            }

        return true;
    }


    ///<summary>
    ///A less strict version of checkValidTile where as long as the tile being checked
    ///is not physically occupied by another entity or claimed by another entity, the tile is
    ///valid. This check is mainly used by attacks or movements which will step into enemy territory.
    ///</summary>
    public bool checkFreeTile(int x, int y)
    {
        Vector3Int coordToCheck = new Vector3Int(x, y, 0);

        StageTile stageTileToCheck = stageHandler.stageTiles
            [stageHandler.stageTilemap.CellToWorld(coordToCheck)];

        CustomTile customTileToCheck = stageHandler.getCustTile(coordToCheck);

        if(stageTileToCheck.entity == this)
        {
            return true;
        }
        
        if(stageHandler.stageTilemap.GetTile
            (coordToCheck) == null
                ||
            stageTileToCheck.entity != null
                ||
            (isGrounded && !customTileToCheck.isPassable)
                ||
            (stageTileToCheck.entityClaimant != null && stageTileToCheck.entityClaimant != this)
            )
            {
                return false;
            }

        return true;
    }


    ///<summary>
    ///Allows an entity to claim additional tiles as occupied by them which will prevent other entities
    ///or effects from moving onto those tiles whilst they are claimed by this entity.
    ///</summary>
    public void ClaimTileOccupancy(int x, int y)
    {

        Vector3Int coordinate = new Vector3Int(x, y, 0);
        StageTile stageTile = stageHandler.stageTiles
            [stageHandler.stageTilemap.CellToWorld(coordinate)];

        if(stageTile.entityClaimant != null)
        {
            print("Tile at " + x + ", " + y + "Has already been claimed by another entity: " + stageTile.entityClaimant.name);
            return;
        }

        stageTile.entityClaimant = this;
        stageTile.isOccupied = true;
        
        claimedTiles.Add(stageTile);

    }

    public void ClaimTileOccupancy(List<Vector3Int> coordinateList)
    {
        foreach(Vector3Int coord in coordinateList)
        {
            StageTile stageTile = stageHandler.stageTiles
            [stageHandler.stageTilemap.CellToWorld(coord)];

            if(stageTile.entityClaimant != null)
            {
                continue;
            }

            stageTile.entityClaimant = this;
            stageTile.isOccupied = true;

            claimedTiles.Add(stageTile);            

        }


    }

    public void ClearClaimedTiles()
    {
        foreach(StageTile stageTile in claimedTiles)
        {
            stageTile.entityClaimant = null;
            stageTile.isOccupied = false;
        }
        claimedTiles.Clear();

    }


    ///<summary>
    ///Wrapper method for cellMove which includes checkValidTile before moving
    ///</summary>
    public void cellMoveVerified(int x, int y)
    {
        if(!checkValidTile(currentCellPos.x + x, currentCellPos.y + y))
        {return;}

        stageHandler.setCellEntity(currentCellPos.x, currentCellPos.y, this, false);
        currentCellPos.Set(currentCellPos.x + x, currentCellPos.y + y, 0);
        stageHandler.setCellEntity(currentCellPos.x, currentCellPos.y, this, true);
        worldTransform.position = stageHandler.stageTilemap.
                                    GetCellCenterWorld(currentCellPos);
    }

    ///<summary>
    ///Moves the entity the given x and y value (x for row, y for column). 
    ///This method does not validate the tile it is attempting to move onto.
    ///</summary>
    public virtual void cellMove(int x, int y)
    {
        stageHandler.setCellEntity(currentCellPos.x, currentCellPos.y, this, false);
        stageHandler.SetPreviousSeenEntity(currentCellPos.x, currentCellPos.y, this, true);
        previousCellPos.Set(currentCellPos.x, currentCellPos.y, 0);

        moveOffTile(currentCellPos.x, currentCellPos.y, this);

        currentCellPos.Set(currentCellPos.x + x, currentCellPos.y + y, 0);

        moveOntoTile(currentCellPos.x, currentCellPos.y, this);
        stageHandler.setCellEntity(currentCellPos.x, currentCellPos.y, this, true);
        worldTransform.position = stageHandler.stageTilemap.
                                    GetCellCenterWorld(currentCellPos);
    }

 
    public Vector3Int getCurrentCellPos()
    {
        return currentCellPos;
    }


    //Old method, needs to be deprecated
    public virtual IEnumerator setStatusEffect(EStatusEffects status, float duration)
    {

       switch (status) 
       {
        case EStatusEffects.Paralyzed:
            if(!isStunnable){yield break;}
            if(nonVolatileStatus){yield break;}
            isStunned = true;
            nonVolatileStatus = true; 
            animator.speed = 0f;
            StartCoroutine(FlashColor(Color.yellow, duration));
            yield return new WaitForSeconds(duration);
            animator.speed = 1f;
            nonVolatileStatus = false;
        break;

        case EStatusEffects.Rooted:

            isRooted = true;
            yield return new WaitForSeconds(duration);
            isRooted = false;
            break;

        case EStatusEffects.Frozen:
            if(!isStunnable){yield break;}
            if(nonVolatileStatus){yield break;}
            isStunned = true;
            nonVolatileStatus = true; 
            animator.speed = 0f;
            yield return new WaitForSeconds(duration);
            animator.speed = 1f;
            nonVolatileStatus = false;

        break;
       }
       Debug.LogWarning("using old setStatusEffect method on BStageEntity, need to switch to StatusEffectManager methods.");

    }

    public IEnumerator FlashColor(Color color, float duration)
    {
        float gracePeriod = duration;

        while (gracePeriod>=0){
            

            setSolidColor(color);
            gracePeriod -= 0.05f;

            yield return new WaitForSeconds(0.05f);
            setNormalSprite();
            gracePeriod -= 0.05f;

            yield return new WaitForSeconds(0.05f);
            
        }

    }





    protected virtual IEnumerator InvincibilityFrames(float duration)
    {
        float gracePeriod = duration;
        fullInvincible = true;

        while (gracePeriod>=0){
            

            spriteRenderer.color = invisible;
            gracePeriod -= 0.05f;

            yield return new WaitForSecondsRealtime(0.05f);
            spriteRenderer.color = opaque;
            gracePeriod -= 0.05f;

            yield return new WaitForSecondsRealtime(0.05f);
            
        }

        fullInvincible = false;
    }


    public void AttemptShove(int x, int y)
    {
        StartCoroutine(Shove(x, y));
    }

///<summary>
///forcefully moves the entity a given distance. Will deal damage to this entity
///if shoved into an obstacle. If the obstacle is another entity, will also deal damage
///to that colliding entity. Damage dealt scales with the strength of the shove. 
///</summary> 
    public IEnumerator Shove(int x, int y, int damage = 40)
    {
        Vector3Int destinationCell = new Vector3Int(currentCellPos.x + x, currentCellPos.y + y, 0);
        Vector3 currentWorldPosition = stageHandler.stageTilemap.GetCellCenterWorld(currentCellPos);
        Vector3 destinationWorldPosition = stageHandler.stageTilemap.GetCellCenterWorld(destinationCell);

        if(!checkFreeTile(destinationCell.x, destinationCell.y) &&
        stageHandler.getEntityAtCell(destinationCell.x, destinationCell.y) == null)
        {yield break;}

        if(stageHandler.getEntityAtCell(destinationCell.x, destinationCell.y) != null)
        {
            //Horizontal Shove
            if(Math.Abs(currentCellPos.x - destinationCell.x) == 1 )
            {
                worldTransform.DOMove(new Vector3((destinationWorldPosition.x - 0.3f), destinationWorldPosition.y, 0), 0.10f ).
                SetEase(Ease.OutCirc).SetUpdate(false);
                isBeingShoved = true;
                isRooted = true;
                

                yield return new WaitForSeconds(0.10f);
                worldTransform.DOMove(currentWorldPosition, 0.15f ).SetEase(Ease.OutExpo).SetUpdate(false);

                hurtEntity(damage, false, true);
                yield return new WaitForSecondsRealtime(0.05f);
                stageHandler.getEntityAtCell(destinationCell.x, destinationCell.y).hurtEntity(damage, false, true);     
                yield return new WaitForSeconds(0.15f);

                isBeingShoved = false;           
                isRooted = false;

            }else
            //Vertical Shove 
            if(Math.Abs(currentCellPos.y - destinationCell.y) == 1)
            {
                worldTransform.DOMove(new Vector3(destinationWorldPosition.x, destinationWorldPosition.y - 0.5f, 0), 0.1f ).SetEase(Ease.OutCirc).SetUpdate(false);
                isBeingShoved = true;

                yield return new WaitForSecondsRealtime(0.1f);
                worldTransform.DOMove(currentCellPos, 0.15f ).SetEase(Ease.OutExpo).SetUpdate(true);
                hurtEntity(damage, false, true);

                yield return new WaitForSecondsRealtime(0.05f);
                stageHandler.getEntityAtCell(destinationCell.x, destinationCell.y).hurtEntity(damage, false, true);     
                yield return new WaitForSeconds(0.1f);

                isBeingShoved = false;                   
                isRooted = false;


            }

            yield break;


        }

        stageHandler.setCellEntity(currentCellPos.x, currentCellPos.y, this, false);
        stageHandler.SetPreviousSeenEntity(currentCellPos.x, currentCellPos.y, this, true);
        previousCellPos.Set(currentCellPos.x, currentCellPos.y, 0);

        moveOffTile(currentCellPos.x, currentCellPos.y, this);

        currentCellPos.Set(currentCellPos.x + x, currentCellPos.y + y, 0);
        isRooted = true;
        worldTransform.DOMove(stageHandler.stageTilemap.GetCellCenterWorld(destinationCell), 0.15f ).SetEase(Ease.OutCubic).SetUpdate(false);
        isBeingShoved = true;
        yield return new WaitForSeconds(0.075f);

        moveOntoTile(currentCellPos.x, currentCellPos.y, this);
        stageHandler.setCellEntity(currentCellPos.x, currentCellPos.y, this, true);

        yield return new WaitForSeconds(0.075f);
        isBeingShoved = false;
        isRooted = false;



    }

    public virtual IEnumerator TweenMove(int x, int y, float duration, Ease easeType)
    {
        if(isMovingCoroutine != null)
        {
            print("moving coroutine not null, cannot move again");
            yield break;
        }
        
        Vector3Int destinationCell = new Vector3Int(currentCellPos.x + x, currentCellPos.y + y, 0);
        if(!checkValidTile(destinationCell.x, destinationCell.y))
        {yield break;}
        if(isRooted || isStunned)
        {yield break;}


        ClaimTileOccupancy(destinationCell.x, destinationCell.y);
        stageHandler.setCellEntity(currentCellPos.x, currentCellPos.y, this, false);
        stageHandler.SetPreviousSeenEntity(currentCellPos.x, currentCellPos.y, this, true);
        previousCellPos.Set(currentCellPos.x, currentCellPos.y, 0);
        moveOffTile(currentCellPos.x, currentCellPos.y, this);

        currentCellPos.Set(currentCellPos.x + x, currentCellPos.y + y, 0);

        if(isPlayer && TimeManager.isCurrentlySlowedDown)
        {
            worldTransform.DOMove(stageHandler.stageTilemap.GetCellCenterWorld(destinationCell), duration*3).SetEase(easeType).SetUpdate(true);
            yield return new WaitForSecondsRealtime((duration*3) * 0.5f);            
        }else
        {
            worldTransform.DOMove(stageHandler.stageTilemap.GetCellCenterWorld(destinationCell), duration).SetEase(easeType);
            yield return new WaitForSeconds(duration * 0.5f);
        }


        moveOntoTile(currentCellPos.x, currentCellPos.y, this);
        stageHandler.setCellEntity(currentCellPos.x, currentCellPos.y, this, true);
        ClearClaimedTiles();

        //isMoving = false;
        isMovingCoroutine = null;
        //yield return null;

    }






    float countFPS = 24;
    float countDuration = 0.12f;
    public IEnumerator animateNumber(int initialNum, int finalNum)
    {
        int startNum = initialNum;
        //print("StartHP: " + startHP);
        int endNum = finalNum;
        //print("EndHP: " + endHP);
        int stepAmount;
        float defaultDelay = countDuration / countFPS;
        int numOfSteps = (int) Math.Round((countDuration/defaultDelay), MidpointRounding.AwayFromZero);
        //print("numOfSteps: " + numOfSteps);

        var difference = Mathf.Abs(startNum - endNum);

        stepAmount = Mathf.CeilToInt(((float)difference/(float)numOfSteps));
        //print("stepAmount: " + stepAmount);
        int updatedNumSteps = difference / stepAmount;
        //print("updatedNumSteps: " + updatedNumSteps);


        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(countDuration/updatedNumSteps);


        int stepCounter = 0;
        if(startNum > endNum)
        {
            while(stepCounter < updatedNumSteps)
            {
                stepCounter++;
                startNum -= stepAmount;
                healthText.text = Mathf.Clamp(startNum, 0, startNum).ToString();
                if(startNum <= 0)
                {
                    healthText.enabled = false;
                    AnimateHPCoroutine = null;
                    yield break;
                    
                }
                yield return wait;

            }
            healthText.text = endNum.ToString();
            stepCounter = 0;

        }else 
        if(startNum<endNum)
        {
            while(stepCounter < updatedNumSteps)
            {
                stepCounter++;
                startNum += stepAmount;
                healthText.text = Mathf.Clamp(startNum, 0, startNum).ToString();



                yield return wait;
            }
            healthText.text = endNum.ToString();
            stepCounter = 0;
    
        }

        AnimateHPCoroutine = null;

    }

    [SerializeField]float HPShakeDuration = 0.12f;
    [SerializeField]float HPShakeStrength = 0.2f;
    [SerializeField]int HPShakeVibrato = 1000;

    //Logic for shaking the HP number when damaged.
    public void AnimateShakeNumber(int damage)
    {
        if(damage <= 10)
        {
            DefaultHPNumberPosition.DOShakePosition(HPShakeDuration, HPShakeStrength*0.5f, HPShakeVibrato).SetUpdate(true);

        }else if(damage < 80 )
        {
            DefaultHPNumberPosition.DOShakePosition(HPShakeDuration*1.5f, HPShakeStrength, HPShakeVibrato).SetUpdate(true);

            
            StartCoroutine(FadeInAndOutColor(Color.white, Color.red, HPShakeDuration*1.5f));
        }else
        {
            DefaultHPNumberPosition.DOShakePosition(HPShakeDuration*2f, HPShakeStrength*2f, HPShakeVibrato).SetUpdate(true);

            StartCoroutine(FadeInAndOutColor(Color.white, Color.red, HPShakeDuration*2f));
        }

    }

    IEnumerator FadeInAndOutColor(Color color1, Color color2, float duration)
    {
        healthText.DOColor(color2, duration*0.6f).SetUpdate(true);
        yield return new WaitForSecondsRealtime(duration*0.5f);
        healthText.DOColor(color1, duration*0.4f).SetUpdate(true);
        //FadeColorCoroutine = null;
        
    }


    public void GrantUntargetableDuration(float duration)
    {
        StartCoroutine(UntargetableBuff(duration));
    }
    IEnumerator UntargetableBuff(float duration)
    {
        isUntargetable = true;
        yield return new WaitForSeconds(duration);
        isUntargetable = false;

    }

}
