using System.Security.Cryptography.X509Certificates;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChipSelectScreenMovement : MonoBehaviour
{

    private Vector3 endPosition = new Vector3 (-690, 170, 0);
    private Vector3 startPosition = new Vector3 (-1229, 170, 0);
    ChipInventory chipInventory;
    int maxSelectableChips = 5;
    [SerializeField] float desiredDuration = 1f;

    private float elapsedTime = 0;
    private RectTransform rectTransform;
    private float journeyLength;

    [SerializeField] List<ChipSO> activeChips = new List<ChipSO>();
    [SerializeField] List<ChipSO> selectableChips;
    [SerializeField] GameObject[] chipButtons;
    [SerializeField] GameObject[] ActiveChipSlots;
    PlayerMovement playerMovement;
    ChipLoadManager chipLoadManager;

    int ActiveChipSlotAccumulator = 0;


    private bool isTriggered = false;
    private bool isActive = false;

    public static bool GameIsPaused = false;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        journeyLength = Vector3.Distance(startPosition, endPosition);
        rectTransform.anchoredPosition = startPosition;
        chipInventory = FindObjectOfType<ChipInventory>();
        playerMovement = FindObjectOfType<PlayerMovement>();
        chipLoadManager = FindObjectOfType<ChipLoadManager>();
        populateChipSelect();
    }

    void Update()
    {
        if (!isActive){return;}

        if(isTriggered)
        {
        elapsedTime += Time.unscaledDeltaTime;
        float percentageComplete = elapsedTime/desiredDuration;

        rectTransform.anchoredPosition = Vector3.Lerp(startPosition, endPosition, percentageComplete);
        }

        if(!isTriggered)
        {
        elapsedTime += Time.unscaledDeltaTime;
        float percentageComplete = elapsedTime/desiredDuration;
        rectTransform.anchoredPosition = Vector3.Lerp(endPosition, startPosition, percentageComplete);
        }


    }

    public void EnableChipMenu()
    {
        Debug.Log("Triggered Menu");
        isActive = true;

        if(!isTriggered)
        {
            Pause();
            isTriggered = true;
            elapsedTime = 0;
            //Debug.Log("Trigger True");
        }
        else if(isTriggered)
        {
            unPause();
            isTriggered = false;
            elapsedTime = 0;
            //Debug.Log("Trigger False");
        }
    }


    void Pause(){
        Time.timeScale = 0f;
        GameIsPaused = true;
    }
    void unPause(){Time.timeScale = 1; GameIsPaused = false;}


    void populateChipSelect()
    {
            foreach(var chip in chipInventory.getChipInventory())
            {
                selectableChips.Add(chip);
            }
            for (int i = 0; i < selectableChips.Count; i++)
            {
                chipButtons[i].GetComponent<ChipSlot>().changeChip(selectableChips[i]);

            }
    }

    public void OnChipSelect(int buttonIndex)
    {
        if(ActiveChipSlotAccumulator == 5)
        {
            return;
        }

        ActiveChipSlots[ActiveChipSlotAccumulator].GetComponent<ChipSlot>().changeChip(selectableChips[buttonIndex]);
        chipButtons[buttonIndex].SetActive(false);
        activeChips.Add(selectableChips[buttonIndex]);
        ActiveChipSlotAccumulator++;
    }

    //OK Button Functionality
    public void LoadIntoChipQueue()
    {
        foreach(ChipSO chip in activeChips)
        {
            chipLoadManager.chipQueue.Add(chip);
        }

            activeChips.Clear();
    

        for (int i = 0; i < ActiveChipSlotAccumulator; i++)
        {
        ActiveChipSlots[i].GetComponent<ChipSlot>().clearChip();
        }

        ActiveChipSlotAccumulator = 0;
        chipLoadManager.calcNextChipLoad(); 
        print("Class: ChipSelectScreenMovement, attempted calcNextChipLoad()");       
    }

}