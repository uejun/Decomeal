using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackMenuButton : MonoBehaviour {

    public void OnClick() {
        LevelManager.Instance.MoveScene("MenuScene");
    }
}
