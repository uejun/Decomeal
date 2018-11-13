using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoodleMenuPanel : MonoBehaviour {

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
            case "醤油濃味":
                NoodleManager.noodleTextureType = NoodleTextureType.thick;
                break;
            case "醤油薄味":
                NoodleManager.noodleTextureType = NoodleTextureType.thin;
                break;
            case "トムヤムクン激辛":
                NoodleManager.noodleTextureType = NoodleTextureType.hot;
                break;
            case "トムヤムクンクリーミー":
                NoodleManager.noodleTextureType = NoodleTextureType.creamy;
                break;
        }
    }
    
}
