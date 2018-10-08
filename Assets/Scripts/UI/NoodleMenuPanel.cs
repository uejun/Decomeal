using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoodleMenuPanel : MonoBehaviour {

    public GameObject ARCamera;

    public void SetMenu(string menu)
    {
        var arMainNoodle = ARCamera.GetComponent<ARMainNoodle>();

        switch (menu)
        {
            case "Spicy":
                //arMainNoodle.SetTargetColor(new ColorTextureCreator(30, 0, 0, 1.0));
                break;
            case "Creamy":
                //arMainNoodle.SetTargetColor(new ColorTextureCreator(150, 0, 0, 1.0));
                break;
        }
    }
}
