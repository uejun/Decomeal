﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackMenuButton : MonoBehaviour {

    private void Update()
    {
        var m_RectTransform = GetComponent<RectTransform>();
        if (Screen.orientation == ScreenOrientation.LandscapeLeft)
        {
            m_RectTransform.localPosition = new Vector3(-160, -Screen.height / 2 + 105, 0);
        }
        else if (Screen.orientation == ScreenOrientation.LandscapeRight)
        {
            m_RectTransform.localPosition = new Vector3(-160 - 200, Screen.height / 2 - 105, 0);

        }
        else
        {
            m_RectTransform.localPosition = new Vector3(-160, -Screen.height / 2 + 105, 0);
        }

    }

    public void OnClick() {
        LevelManager.Instance.MoveScene("MenuScene");
    }
}
