using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ARManualPanel : MonoBehaviour {

	void Start () {
		
	}
	
	void Update () {
		if (Input.GetKey(KeyCode.L)) {
			GameObject.Find ("IsTargetLockedInText").GetComponent<Text> ().text = "LockedIn";
		}

		if (Input.GetKey(KeyCode.U)) {
			GameObject.Find ("IsTargetLockedInText").GetComponent<Text> ().text = "UnLockedIn";
		}

		if (Input.GetKey(KeyCode.P)) {
			GameObject.Find ("IsTargetLockedInText").GetComponent<Text> ().text = "OnPlate";
		}
		
		if (Input.GetKey(KeyCode.O)) {
			GameObject.Find ("IsTargetLockedInText").GetComponent<Text> ().text = "NotOnPlate";
		}

	}
}
