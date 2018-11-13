using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NoodleType {
    normal, tomyam
}

public enum NoodleTextureType {
    thick, thin, hot, creamy
}

public class NoodleManager : MonoBehaviour {

    static public NoodleType noodleType = NoodleType.normal;
    static public NoodleTextureType noodleTextureType = NoodleTextureType.thick;

}
