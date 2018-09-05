using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;

public class ARTexture : MonoBehaviour {

	public GameObject textureCreator;
	private TextureCreator _textureCreator;

	// Use this for initialization
	void Start () {
		_textureCreator = textureCreator.GetComponent<SushiTextureCreator> ();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void changeNext() {
		print("change!");
		_textureCreator.changeNext();
	}
}
