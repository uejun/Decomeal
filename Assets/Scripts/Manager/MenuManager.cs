using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour {

    public void OnMenuClick(string sceneName) {
        LevelManager.Instance.MoveScene(sceneName);
    }
}
