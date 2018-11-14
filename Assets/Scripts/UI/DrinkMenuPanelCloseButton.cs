using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrinkMenuPanelCloseButton : MonoBehaviour {

    private void Update()
    {
        var m_RectTransform = GetComponent<RectTransform>();
        if (Screen.orientation == ScreenOrientation.LandscapeLeft)
        {
            m_RectTransform.anchorMax.Set(1.0f, 1.0f);
            m_RectTransform.anchorMin.Set(1.0f, 1.0f);
            m_RectTransform.anchoredPosition = new Vector3(-60, -73, 0);
        }
        else if (Screen.orientation == ScreenOrientation.LandscapeRight)
        {
            m_RectTransform.anchorMax.Set(1.0f, 1.0f);
            m_RectTransform.anchorMin.Set(1.0f, 1.0f);

            m_RectTransform.anchoredPosition = new Vector3(-800 + 60, -73, 0);
        }
        else
        {
            m_RectTransform.anchorMax.Set(1.0f, 1.0f);
            m_RectTransform.anchorMin.Set(1.0f, 1.0f);
            m_RectTransform.anchoredPosition = new Vector3(-60, -73, 0);

        }

    }

    public void OnClick() {
        GameObject.Find("DrinkMenuPanel").SetActive(false);
    }
}
