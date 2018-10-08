using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrinkMenuPanel : MonoBehaviour {

    public GameObject ARCamera;

    public void SetDrinkMenu(string menu) {
        var arMainDrink = ARCamera.GetComponent<ARMainDrink>();

        switch(menu) {
            case "Lemon":
                arMainDrink.SetTargetColor(new ColorTextureCreator(30, 0, 0, 1.0));
                break;
            case "Grape":
                arMainDrink.SetTargetColor(new ColorTextureCreator(150, 0, 0, 1.0));
                break;
            case "Peach":
                arMainDrink.SetTargetColor(new ColorTextureCreator(20, 0, 0, 1.0));
                break;
        }
    }
}
