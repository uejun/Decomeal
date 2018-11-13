using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SushiMenuPanel : MonoBehaviour {

    public GameObject menuSelectButton1;
    public GameObject menuSelectButton2;
    public GameObject menuSelectButton3;
    public GameObject menuSelectButton4;

    void Start()
    {
        gameObject.SetActive(false);
    }

    public void Open()
    {

        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void SetMenu(string menu)
    {

        switch (menu)
        {
            case "大トロ":
                SushiManager.sushiTextureType = SushiTextureType.otoro;
                break;
            case "中トロ":
                SushiManager.sushiTextureType = SushiTextureType.tyutoro;
                break;
            case "サーモン":
                SushiManager.sushiTextureType = SushiTextureType.sarmon;
                break;
            case "ハマチ":
                SushiManager.sushiTextureType = SushiTextureType.hamachi;
                break;
        }
    }
}
