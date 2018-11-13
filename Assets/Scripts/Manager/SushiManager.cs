using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum SushiTextureType
{
    otoro, tyutoro, sarmon, hamachi, akami
}

public class SushiManager : MonoBehaviour
{

    static public SushiTextureType sushiTextureType = SushiTextureType.otoro;
}
