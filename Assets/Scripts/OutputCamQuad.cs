using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;

public class OutputCamQuad : MonoBehaviour {

	Texture2D outputTexture;
	Mat outputMat;

	GameObject outputQuad;
	Camera outputCamera;


	void Start () {
        outputTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false);
        outputQuad = transform.Find ("QuadForOutput").gameObject;
		outputQuad.transform.localScale = new Vector3 (outputTexture.width, outputTexture.height, outputQuad.transform.localScale.z);
		outputQuad.GetComponent<Renderer> ().material.mainTexture = outputTexture;
		
		outputCamera = GetComponent<Camera> ();
    
		outputCamera.orthographicSize = outputTexture.height / 2;

		outputMat = new Mat (Screen.height, Screen.width, CvType.CV_8UC3);
	}

	void Update () {
		
	}

	public void setMat(Mat mat) {
		var _mat = mat.clone();
		
		if(_mat.channels() == 1) {
			Mat bgrMat = new Mat (_mat.size (), CvType.CV_8UC3);
			Imgproc.cvtColor (_mat, bgrMat, Imgproc.COLOR_GRAY2RGB);
            Imgproc.resize(bgrMat, outputMat, outputMat.size());
			//outputMat = bgrMat;
		} else {
            Imgproc.resize(_mat, outputMat, outputMat.size());
            //outputMat = _mat;
		}
		
		Utils.matToTexture2D (outputMat, outputTexture);
	}

}
