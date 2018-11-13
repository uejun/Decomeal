using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;

public class ARUtil : MonoBehaviour {

	public static double norm_L1 (Point pt1, Point pt2) {
		return Mathf.Abs ((float)(pt1.x - pt2.x)) + Mathf.Abs ((float)(pt1.y - pt2.y));
	}

	// rectの最小・最大をmat領域内におさめたRectを得る
	public static OpenCVForUnity.Rect calcRectWithinMat (OpenCVForUnity.Rect rect, Mat mat)
	{
		int minLimitX = (int)Mathf.Min (Mathf.Max ((float)0.0, (float)rect.tl ().x), (float)mat.cols ());
		int maxLimitX = (int)Mathf.Min (Mathf.Max ((float)0.0, (float)rect.br ().x), (float)mat.cols ());
		int minLimitY = (int)Mathf.Min (Mathf.Max ((float)0.0, (float)rect.tl ().y), (float)mat.rows ());
		int maxLimitY = (int)Mathf.Min (Mathf.Max ((float)0.0, (float)rect.br ().y), (float)mat.rows ());
		return new OpenCVForUnity.Rect (minLimitX, minLimitY, maxLimitX - minLimitX, maxLimitY - minLimitY);
	}

	// オブジェクトの型をコンソールに出力する
	public static void showType (object o)
	{
		Debug.Log (o.GetType ());
	}


	public static void affineTransform (Mat src, Mat dst, OpenCVForUnity.Rect roi)
	{
		// アフィン行列を取得
		var srcPoints = new MatOfPoint2f (new Point (0.0, 0.0), new Point (src.cols () - 1, 0.0), new Point (src.cols () - 1, src.rows () - 1));
		var dstPoints = new MatOfPoint2f (roi.tl (), new Point (roi.x + roi.width, roi.y), roi.br ());
		Mat transform = Imgproc.getAffineTransform (srcPoints, dstPoints);

		// アフィン変換
		Imgproc.warpAffine (src, dst, transform, dst.size (), Imgproc.INTER_LINEAR);
	}

	public static void affineTransform (Mat src, Mat dst, OpenCVForUnity.RotatedRect roi)
	{
		Point[] pts = new Point[4];
		roi.points(pts);

		// アフィン行列を取得
		var srcPoints = new MatOfPoint2f (new Point (0.0, 0.0), new Point (src.cols () - 1, 0.0), new Point (src.cols () - 1, src.rows () - 1));
		var dstPoints = new MatOfPoint2f (pts[0], pts[1], pts[2]);
		Mat transform = Imgproc.getAffineTransform (srcPoints, dstPoints);

		// アフィン変換
		Imgproc.warpAffine (src, dst, transform, dst.size (), Imgproc.INTER_LINEAR);
	}

	// リージョンは, アルファブレンドする領域のマスクに使用. また処理高速化のためROIに対して処理を行うためにもRegionを利用する
	public static void alphaBlend (Mat src1, Mat src2, double alpha, Region region) {
		Mat roiSrc1 = new Mat (src1, region.rect);
		Mat roiSrc2 = new Mat (src2, region.rect);

		double beta = 1.0 - alpha;

		Mat dst = new Mat (src1.size (), CvType.CV_8UC3);
		Mat roiDst = new Mat (dst, region.rect);

		Core.addWeighted (roiSrc1, alpha, roiSrc2, beta, 0.0, roiDst);

		roiDst.copyTo (roiSrc1, region.roiMask);
	}

	public static void alphaBlend (Mat src1, Mat src2, double alpha, Mat mask) {
		double beta = 1.0 - alpha;

		Mat dst = new Mat (src1.size (), src1.type());
	
		Core.addWeighted (src1, alpha, src2, beta, 0.0, dst);

		dst.copyTo (src1, mask);
	}

	public static void writeRotatedRect (Mat mat, RotatedRect rRect)
	{
		Point[] vertices = new Point[4];
		rRect.points (vertices);
		for (int i = 0; i < 4; i++) {
			Imgproc.line (mat, vertices [i], vertices [(i + 1) % 4], new Scalar (0, 255, 0));
		}
	}

	public static List<Mat> getRGBChannels (Mat rgbMat)
	{
		var channels = new List<Mat> () {
			new Mat (rgbMat.size (), CvType.CV_8UC1),
			new Mat (rgbMat.size (), CvType.CV_8UC1),
			new Mat (rgbMat.size (), CvType.CV_8UC1)
		};
		Core.split (rgbMat, channels);
		return channels;
	}

	public static List<Mat> getYCrCbChannels (Mat rgbMat)
	{
		Mat yCrCbMat = new Mat (rgbMat.size (), CvType.CV_8UC3);
		Imgproc.cvtColor (rgbMat, yCrCbMat, Imgproc.COLOR_RGB2YCrCb);
		
		var channels = new List<Mat> () {
			new Mat (yCrCbMat.size (), CvType.CV_8UC1),
			new Mat (yCrCbMat.size (), CvType.CV_8UC1),
			new Mat (yCrCbMat.size (), CvType.CV_8UC1)
		};

		Core.split (yCrCbMat, channels);
		return channels;
	}

	public static List<Mat> getHSVChannels (Mat rgbMat)
	{
		Mat hsvMat = new Mat (rgbMat.size (), CvType.CV_8UC3);
		Imgproc.cvtColor (rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);

		var channels = new List<Mat> () {
			new Mat (rgbMat.size (), CvType.CV_8UC1),
			new Mat (rgbMat.size (), CvType.CV_8UC1),
			new Mat (rgbMat.size (), CvType.CV_8UC1)
		};
		Core.split (hsvMat, channels);
		return channels;
	}

	public static int getMaxAreaIndex (List<MatOfPoint> contours)
	{
		double maxArea = 0;
		int maxAreaIndex = 0;
		for (int i = 0; i < contours.Count; i++) {
			double area = Imgproc.contourArea (contours [i]);
			if (maxArea < area) {
				maxArea = area;
				maxAreaIndex = i;
			}
		}
		return maxAreaIndex;
	}

	// 輪郭探索
	public static List<MatOfPoint> findContours (Mat mat, Point offset)
	{
		var contours = new List<MatOfPoint> ();
		Mat hierarchy = Mat.zeros (new Size (5, 5), CvType.CV_8UC1);
		Imgproc.findContours (mat, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE, offset);
		return contours;
	}

	// 輪郭探索
	public static List<MatOfPoint> findContours (Mat mat)
	{
		var contours = new List<MatOfPoint> ();
		Mat hierarchy = Mat.zeros (new Size (5, 5), CvType.CV_8UC1);
		Imgproc.findContours (mat, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);
		return contours;
	}

	// 近さの度合いを0-1で算出
	public static double clothness (Point pt1, Point pt2, double max)
	{
		double distance = norm_L1 (pt1, pt2);
		return (1 - distance / max);
	}

	// Rectの中心座標を返す
	public static Point getRectCenterPoint (OpenCVForUnity.Rect rect)
	{
		return new Point (rect.x + rect.width / 2, rect.y + rect.height / 2);
	}

	public static void contourProcess(Mat texture, Mat dst, Mat contoursMat){
		Core.addWeighted (texture, 0.9, contoursMat, 0.1, 0.0, dst);
	}
}
