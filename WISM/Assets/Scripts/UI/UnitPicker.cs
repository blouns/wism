using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitPicker : MonoBehaviour
{
    public GameObject UnitPickerPanel;

    public void ToggleUnitButton()
    {
        Debug.Log("Toggled unit.");
    }

    public void OkButton()
    {
        Debug.Log("Clicked OK!");
        this.UnitPickerPanel.SetActive(false);
    }
}
