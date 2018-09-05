using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using OpenCVForUnity;


public interface ITextureSource
{
	Mat mat { get; }
}


public class ImageSource: ITextureSource
{
	Mat _mat;

	public Mat mat {
		get { 
			return _mat;
		}
	}

	public ImageSource()
	{
		_mat = Imgcodecs.imread (Utils.getFilePath ("fukakyon_1.jpg"));
	}


	public ImageSource (Texture2D texture)
	{
		_mat = new Mat (texture.height, texture.width, CvType.CV_8UC3); 
		Utils.texture2DToMat (texture, _mat);
	}

}


public class VideoSource: ITextureSource
{

	VideoCapture capture;

	public Mat mat {
		get {
			//Loop play
			if (capture.get (Videoio.CAP_PROP_POS_FRAMES) >= capture.get (Videoio.CAP_PROP_FRAME_COUNT))
				capture.set (Videoio.CAP_PROP_POS_FRAMES, 0);

			//error PlayerLoop called recursively! on iOS.reccomend WebCamTexture.
			var _mat = new Mat ();
			if (capture.grab ()) {

				capture.retrieve (_mat, 0);
				Imgproc.cvtColor (_mat, _mat, Imgproc.COLOR_BGR2RGB);
				return _mat;
			}
			return _mat;
		}
	}

	public VideoSource ()
	{
		capture = new VideoCapture ();
		capture.open (Utils.getFilePath ("aulait_short.mov"));

		if (capture.isOpened ()) {
			Debug.Log ("VideoOpen");
		} else { // TODO: Errorハンドリング
			Debug.Log ("capture.isOpened() false");	
			capture.release ();
		}
	}

	~VideoSource ()
	{
		capture.release ();
	}

	public void createTextMat()
	{
		return;
	}
}