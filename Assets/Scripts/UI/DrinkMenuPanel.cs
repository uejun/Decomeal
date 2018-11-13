using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrinkMenuPanel : MonoBehaviour {

    public GameObject DrinkManager;

    public void SetDrinkMenu(string menu) {
        var arMainDrink = DrinkManager.GetComponent<ARDrink>();

        switch(menu) {
            case "Lemon":
                arMainDrink.SetTargetColor(new ColorTextureCreator(30, 0, 0, 1.0));
                break;
            case "Grape":
                arMainDrink.SetTargetColor(new ColorTextureCreator(150, 0, 0, 1.0));
                break;
            case "Wanashi":
                arMainDrink.SetTargetColor(new ColorTextureCreator(60, 0, 0, 1.0));
                break;
        }
    }
}
