using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InspectorArrayLayout
{
    public static int rowSize = 7;


    [System.Serializable]
    public struct rowData
    {
        public bool[] row;
    }

    public rowData[] rows = new rowData[rowSize]; //Grid of 7x7

}
