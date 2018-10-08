using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrinkMenuPanelCloseButton : MonoBehaviour {

    public void OnClick() {
        GameObject.Find("DrinkMenuPanel").SetActive(false);
    }
}
