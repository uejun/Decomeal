using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;

public interface ARTargetTracker {
	Mat binalize(Mat cameraMat, OpenCVForUnity.Rect searchRect, int threshlod1, int threshold2);
}


public class MaguroTracker: ARTargetTracker {

    int Cr_THRESH_LOW = 150;
    int V_THRESH_LOW = 150;
	int H_THRESH_LOW = 5;
	int H_THRESH_UP = 20;
	int S_THRESH_LOW = 80;

	public Mat binalize(Mat cameraMat, OpenCVForUnity.Rect searchRect, int threshlod1, int threshlod2) {
		
		// 入力画像から探索領域のROI画像を作成
		Mat roiMat = new Mat (cameraMat, searchRect);

		// HSVチャンネル作成. 二値化するため
		var hsvChannels = ARUtil.getHSVChannels(roiMat);
        var yCrCbChannels = ARUtil.getYCrCbChannels(roiMat);

        Mat binaryCr = binalizeByCr(yCrCbChannels[1], roiMat.size(), threshlod1);
        Mat binaryS = binalizeByS(hsvChannels [1], roiMat.size(), threshlod2);
		// Imgproc.adaptiveThreshold (hsvChannels [2], coffeeBinaryMat, 255, Imgproc.ADAPTIVE_THRESH_MEAN_C, Imgproc.THRESH_BINARY_INV, 5, 5);

		Mat wiseAnd = new Mat (roiMat.size (), CvType.CV_8UC1);
		Core.bitwise_and(binaryCr, binaryS, wiseAnd);

		// ノイズ除去
		Imgproc.morphologyEx(wiseAnd, wiseAnd, Imgproc.MORPH_OPEN, new Mat(), new Point(-1, -1), 2);

		// 膨張させて輪郭抽出を微修正
		// Imgproc.dilate (wiseAnd, wiseAnd, Imgproc.getStructuringElement (Imgproc.MORPH_RECT, new Size (7, 7)));

		return wiseAnd;
	}

    Mat binalizeByCr(Mat chan, Size size, int threshold) {
        Mat lower = new Mat(size, CvType.CV_8UC1);
        Imgproc.threshold(chan, lower, threshold, 255, Imgproc.THRESH_BINARY);
        return lower;
    }

    Mat binalizeByV(Mat chan, Size size, int threshold)
    {
        Mat lower = new Mat(size, CvType.CV_8UC1);
        Imgproc.threshold(chan, lower, threshold, 255, Imgproc.THRESH_BINARY);
        return lower;
    }

    Mat binalizeByH(Mat chan, Size size) {
		Mat lower = new Mat (size, CvType.CV_8UC1);
		Imgproc.threshold (chan, lower, H_THRESH_LOW, 255, Imgproc.THRESH_BINARY);

		Mat upper = new Mat (size, CvType.CV_8UC1);
		Imgproc.threshold (chan, upper, H_THRESH_UP, 255, Imgproc.THRESH_BINARY_INV);

		Mat and = new Mat (size, CvType.CV_8UC1);
		Core.bitwise_and(lower, upper, and);

		return and;
	}

	Mat binalizeByS(Mat chan, Size size, int threshold) {
		Mat lower = new Mat (size, CvType.CV_8UC1);
		Imgproc.threshold (chan, lower, threshold, 255, Imgproc.THRESH_BINARY);
		return lower;
	}
}
