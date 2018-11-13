using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;


public class BinaryMatCreator {

    // デフォルト値
    double cr_thresh_lower = 150;
    double cr_thresh_upper = 200;
    double v_thresh_lower = 150;
    double v_thresh_upper = 200;
    double s_thresh_lower = 200;
    double s_thresh_upper = 230;

    public void setCrUpper(double val) {
        cr_thresh_upper = val;
    }

    public void setCrLower(double val) {
        cr_thresh_lower = val;
    }

    public void setVUpper(double val) {
        v_thresh_upper = val;
    }

    public void setVLower(double val) {
        v_thresh_lower = val;
    }

    public void setSUpper(double val) {
        s_thresh_upper = val;
    }

    public void setSLower(double val) {
        s_thresh_lower = val;
    }

    public Mat createBinaryMat(Mat cameraMat, OpenCVForUnity.Rect rect) {

        // ROIを絞る
        Mat roiMat = new Mat(cameraMat, rect);

        // 二値化するためのチャンネル群
        var hsvChannels = ARUtil.getHSVChannels(roiMat);
        var yCrCbChannels = ARUtil.getYCrCbChannels(roiMat);

        Mat S_Binary = new Mat(roiMat.size(), CvType.CV_8UC1);
        Core.inRange(hsvChannels[1], new Scalar(s_thresh_lower), new Scalar(s_thresh_upper), S_Binary);

        Mat Cr_Binary = new Mat(roiMat.size(), CvType.CV_8UC1);
        Core.inRange(yCrCbChannels[1], new Scalar(cr_thresh_lower), new Scalar(cr_thresh_upper), Cr_Binary);

        Mat V_Binary = new Mat(roiMat.size(), CvType.CV_8UC1);
        Core.inRange(hsvChannels[2], new Scalar(v_thresh_lower), new Scalar(v_thresh_upper), V_Binary);

        // TODO: adaptiveThresholdの有効利用
        // Imgproc.adaptiveThreshold (hsvChannels [2], coffeeBinaryMat, 255, Imgproc.ADAPTIVE_THRESH_MEAN_C, Imgproc.THRESH_BINARY_INV, 5, 5);

        // 上記二つの二値画像のAndを取る
        Mat wiseAnd = new Mat(roiMat.size(), CvType.CV_8UC1);
        Core.bitwise_and(S_Binary, Cr_Binary, wiseAnd);
        Core.bitwise_and(V_Binary, wiseAnd, wiseAnd);

        // ノイズ除去
        //Imgproc.morphologyEx(wiseAnd, wiseAnd, Imgproc.MORPH_OPEN, new Mat(), new Point(-1, -1), 2);
        
        return wiseAnd;
    }

}
