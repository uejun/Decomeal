using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;

public class OutputCamQuad : MonoBehaviour {

	Texture2D outputTexture;
	Mat outputMat;

	GameObject outputQuad;
	Camera outputCamera;

    int cvType;
    int height;
    int width;

	void Start () {
        setupScreenQuadAndCamera(Screen.height, Screen.width, CvType.CV_8UC3);
	}

    public void setupScreenQuadAndCamera(int height, int width, int type) {
        cvType = type;
        this.height = height;
        this.width = width;

        outputTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        outputQuad = transform.Find("QuadForOutput").gameObject;

        // 出力先画面Quadのサイズ決定
        outputQuad.transform.localScale = new Vector3(outputTexture.width, outputTexture.height, outputQuad.transform.localScale.z);
        // 出力先画面Quadのテクスチャにセット
        outputQuad.GetComponent<Renderer>().material.mainTexture = outputTexture;
        // Quadを撮るカメラ
        outputCamera = GetComponent<Camera>();
        // Quadを撮るカメラのサイズ修正
        outputCamera.orthographicSize = outputTexture.height / 2;

        // Texture2Dに変換する直前のMat
        outputMat = new Mat(height, width, type);
    }

	void Update () {
		
	}

	public void setMat(Mat mat) {

        var _mat = mat.clone();

		if(_mat.channels() == 1) {
            outputMat = new Mat(new Size(Screen.width, Screen.height), CvType.CV_8UC3);
            var _matC3 = new Mat(_mat.size(), CvType.CV_8UC3);
            Imgproc.cvtColor (_mat, _matC3, Imgproc.COLOR_GRAY2RGB);
            Imgproc.resize(_matC3, outputMat, outputMat.size());

			//outputMat = bgrMat;
        } else if (_mat.channels() == 3) {
            outputMat = new Mat(new Size(Screen.width, Screen.height), CvType.CV_8UC3);
            Imgproc.resize(_mat, outputMat, outputMat.size());
        }
        else {
            outputMat = new Mat(new Size(Screen.width, Screen.height), CvType.CV_8UC3);
            var _matC3 = new Mat(_mat.size(), CvType.CV_8UC3);
            Imgproc.cvtColor(_mat, _matC3, Imgproc.COLOR_RGBA2RGB);
            Imgproc.resize(_matC3, outputMat, outputMat.size());
            //outputMat = _mat;
        }

        //Utils.matToTexture2D(outputMat, outputTexture, colors);
        Utils.matToTexture2D (outputMat, outputTexture);
	}

}
