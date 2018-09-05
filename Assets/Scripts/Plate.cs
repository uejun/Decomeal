using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;
using OpenCVForUnity;

public class Plate {

	public List<Vector3> pointsOnWorld = new List<Vector3> ();
	public List<Vector3> pointsOnScreen = new List<Vector3> ();
	public List<Point> pointsOnCvScreen = new List<Point> ();

	private RotatedRect ellipseRotatedRect;
	private ImageTargetBehaviour targetBehaviour;
	private Vector3[] pointsOnLocal;

	private RotatedRect topEllipseRotatedRect;

	// Unityのスクリーン座標系とOpenCVの画像座標系のy軸方向が逆なので、その変換に使用する
	private int screenHeight;

	public Plate (ImageTargetBehaviour targetBehaviour, int screenHeight)
	{
		this.targetBehaviour = targetBehaviour;
		this.screenHeight = screenHeight;

		var halfLen = 0.3f; // 本来0.5fだけど、ちょっと大きいので.
		pointsOnLocal = new Vector3[] {
			new Vector3 (-halfLen, 0.0f, halfLen),
			new Vector3 (halfLen, 0.0f, halfLen),
			new Vector3 (halfLen, 0.0f, -halfLen),
			new Vector3 (-halfLen, 0.0f, -halfLen),
		};

	}

	public void update ()
	{
		// convert the local point to world coordinates
		pointsOnWorld = new List<Vector3> ();
		foreach (var pt in pointsOnLocal) {
			pointsOnWorld.Add (targetBehaviour.transform.TransformPoint (pt));
		}

		// project the world coordinates to screen coords (pixels)
		pointsOnScreen = new List<Vector3> ();
		foreach (var pt in pointsOnWorld) {
			pointsOnScreen.Add (Camera.main.WorldToScreenPoint (pt));
		}

		// 皿の上面のスクリーン座標を計算. y座標が逆向き.
		pointsOnCvScreen = new List<Point> ();
		foreach (var pt in pointsOnScreen) {
			pointsOnCvScreen.Add (new Point (pt.x, this.screenHeight - pt.y));
		}

		ellipseRotatedRect = Imgproc.fitEllipse (new MatOfPoint2f (pointsOnCvScreen.ToArray ()));
	}

	public bool isNotFound ()
	{
		return targetBehaviour.CurrentStatus == TrackableBehaviour.Status.NO_POSE;
	}

	public bool isTracked ()
	{
		return targetBehaviour.CurrentStatus == TrackableBehaviour.Status.TRACKED;
	}

	// カメラを太陽に見立てて、皿の上面とカメラの南中高度. 0度->0, 90度->1.
	public double culminationAltitude ()
	{
		return ellipseRotatedRect.size.width / ellipseRotatedRect.size.height;
	}

	// 皿の上面の点群の外接矩形を取得. OpenCVの画像座標系
	public OpenCVForUnity.Rect getTopSurfaceRect ()
	{
		return Imgproc.boundingRect (new MatOfPoint (pointsOnCvScreen.ToArray ()));
	}

	// 皿の上面の点群の外接矩形を取得. OpenCVの画像座標系. ただし、ROIが画像外の座標になることもあるので、ROIの最小・最大を画像領域内におさめる
	public OpenCVForUnity.Rect getTopSurfaceRect (Mat inMat)
	{
		var broadROI = getTopSurfaceRect ();
		// ROIが画像外の座標になることもあるので、ROIの最小・最大を画像領域内におさめる
		return ARUtil.calcRectWithinMat (broadROI, inMat);
	}

	//皿の上面の点群の最小外接矩形を取得. 
	public RotatedRect getTopSurfaceRotatedRect ()
	{
		return Imgproc.fitEllipse (new MatOfPoint2f (pointsOnCvScreen.ToArray ()));
	}
}
