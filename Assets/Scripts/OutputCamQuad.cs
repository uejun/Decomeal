using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;

public class OutputCamQuad : MonoBehaviour {

	Texture2D outputTexture;
	Mat outputMat;

	GameObject outputQuad;
	Camera outputCamera;

	Color32[] colors;

	void Start () {
		outputTexture = new Texture2D (1024, 768, TextureFormat.ARGB32, false);
		outputQuad = transform.Find ("QuadForOutput").gameObject;
		outputQuad.transform.localScale = new Vector3 (outputTexture.width, outputTexture.height, outputQuad.transform.localScale.z);
		outputQuad.GetComponent<Renderer> ().material.mainTexture = outputTexture;
		colors = new Color32[outputTexture.width * outputTexture.height];

		outputCamera = GetComponent<Camera> ();
		outputCamera.orthographicSize = outputTexture.height / 2;

		outputMat = new Mat (768, 1024, CvType.CV_8UC3);
	}

	void Update () {
		
	}

	public void setMat(Mat mat) {
		var _mat = mat.clone();
		
		if(_mat.channels() == 1) {
			Mat bgrMat = new Mat (_mat.size (), CvType.CV_8UC3);
			Imgproc.cvtColor (_mat, bgrMat, Imgproc.COLOR_GRAY2RGB);
			outputMat = bgrMat;
		} else {
			outputMat = _mat;
		}
		
		Utils.matToTexture2D (outputMat, outputTexture);
	}

}
