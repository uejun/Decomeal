using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;

// 領域候補オブジェクトのリストを受け取り、それぞれに尤もらしさを評価して点数を付けるインタフェース
public interface IScorer
{
	void score (List<RegionCandidate> candidates);
	void setLocation(int x, int y);
}


public class CoffeeScorer: IScorer
{
	/* 戦略
	 * 
	 * (w1*円形度) +(w2*輪郭面積) + (w3*画像中心からの近さ) + (w4*前フレームの領域からの近さ)
	 * 
	 * 円形度: コーヒーカップの上面なので円形の方が高得点
	 * 輪郭面積: 領域が大きい方が該当領域である可能性が高いはず
	 * 画像中心からの距離： 目的領域は画像中心にある可能性が高いはず
	 * 前フレームの領域からの近さ: 急に領域が飛ぶことはないはず
	*/


	public double w_circularity = 0.8;    // 円形度の重み
	public double w_area = 0.01;          // 輪郭面積の重み
	public double w_center = 0.2;         // 画像中心からの近さの重み
	public double w_previousFrame = 0.2;  // 前フレームの領域からの近さの重み

	OpenCVForUnity.Rect searchRect;
	OpenCVForUnity.Rect previousRect;

	public CoffeeScorer (OpenCVForUnity.Rect searchRect, OpenCVForUnity.Rect previousRect)
	{
		this.searchRect = searchRect;
		this.previousRect = previousRect;
	}

	// 重みは外部から変更できる
	public void setWeight (double w_circularity, double w_area, double w_center, double w_previousFrame)
	{
		this.w_circularity = w_circularity;
		this.w_area = w_area;
		this.w_center = w_center;
		this.w_previousFrame = w_previousFrame;
	}

	public void score (List<RegionCandidate> candidates)
	{
		double max = searchRect.area ();

		Point preCenter = ARUtil.getRectCenterPoint (previousRect);

		Point centerPt = ARUtil.getRectCenterPoint (searchRect);
		double maxDistance = (searchRect.width + searchRect.height);

		foreach (var candidate in candidates) {
			candidate.ellipse (); // 外接楕円を計算
			// 円形度 + 輪郭面積 + 画像中心からの近さ + 前フレームの領域からの近さ
			candidate.score = w_circularity * candidate.circularity 
				+ w_area * candidate.area / max 
				+ w_center * ARUtil.clothness (centerPt, candidate.center, maxDistance) 
				+ w_previousFrame * ARUtil.clothness (preCenter, candidate.center, maxDistance);
		}
	}

	public void setLocation(int x, int y) {
	}

}

public class CoffeeWithCupScorer: IScorer
{

	OpenCVForUnity.Rect searchRect;

	public CoffeeWithCupScorer (OpenCVForUnity.Rect searchRect)
	{
		this.searchRect = searchRect;
	}


	// 注: 破壊的メソッド
	public void score (List<RegionCandidate> candidates)
	{
		// roiの中心
		Point roiCenter = ARUtil.getRectCenterPoint (searchRect);
		double maxDistance = (searchRect.width + searchRect.height);

		foreach (var candidate in candidates) {
			candidate.ellipse ();
			candidate.score = ARUtil.clothness (roiCenter, candidate.center, maxDistance);
		}
	}

	public void setLocation(int x, int y) {
	}

}

public class MaguroScorer: IScorer {

	OpenCVForUnity.Rect searchRect;

	bool shouldUseLocation = false;
	int locationX = 0;
	int locationY = 0;

	public MaguroScorer (OpenCVForUnity.Rect searchRect)
	{
		this.searchRect = searchRect;
	}

	public void score (List<RegionCandidate> candidates)
	{
		foreach (var candidate in candidates) {
			candidate.ellipse ();
			candidate.calcArea();
			candidate.score = normalizeArea(candidate) + degreeOfCentering(candidate);
			if (shouldUseLocation) {
				if (!candidate.ellipseRotatedRect.boundingRect().contains(locationX, locationY)) {
					candidate.score -= 100;
				}
			}
		}
		shouldUseLocation = false;
	}

	public void setLocation(int x, int y) {
		locationX = x;
		locationY = y;
		shouldUseLocation = true;
	}

	double normalizeArea(RegionCandidate candidate) {
		var maxArea = searchRect.area();
		candidate.calcArea();
		return candidate.area / maxArea;
	}

	double degreeOfCentering(RegionCandidate candidate) {
		// roiの中心
		Point roiCenter = ARUtil.getRectCenterPoint (searchRect);
		double maxDistance = (searchRect.width + searchRect.height);
		return ARUtil.clothness (roiCenter, candidate.center, maxDistance);
	}
}


public class OrangeScorer : IScorer
{
    /* 戦略
     * 
     * (w1*円形度) +(w2*輪郭面積) + (w3*画像中心からの近さ) + (w4*前フレームの領域からの近さ)
     * 
     * 円形度: コーヒーカップの上面なので円形の方が高得点
     * 輪郭面積: 領域が大きい方が該当領域である可能性が高いはず
     * 画像中心からの距離： 目的領域は画像中心にある可能性が高いはず
     * 前フレームの領域からの近さ: 急に領域が飛ぶことはないはず
    */


    public double w_circularity = 0.8;    // 円形度の重み
    public double w_area = 0.01;          // 輪郭面積の重み
    public double w_center = 0.2;         // 画像中心からの近さの重み
    public double w_previousFrame = 0.2;  // 前フレームの領域からの近さの重み

    OpenCVForUnity.Rect searchRect;
    OpenCVForUnity.Rect previousRect;

    public OrangeScorer(OpenCVForUnity.Rect searchRect, OpenCVForUnity.Rect previousRect)
    {
        this.searchRect = searchRect;
        this.previousRect = previousRect;
    }

    // 重みは外部から変更できる
    public void setWeight(double w_circularity, double w_area, double w_center, double w_previousFrame)
    {
        this.w_circularity = w_circularity;
        this.w_area = w_area;
        this.w_center = w_center;
        this.w_previousFrame = w_previousFrame;
    }

    public void score(List<RegionCandidate> candidates)
    {
        double max = searchRect.area();

        Point preCenter = ARUtil.getRectCenterPoint(previousRect);

        Point centerPt = ARUtil.getRectCenterPoint(searchRect);
        double maxDistance = (searchRect.width + searchRect.height);

        foreach (var candidate in candidates)
        {
            candidate.ellipse(); // 外接楕円を計算
                                 // 円形度 + 輪郭面積 + 画像中心からの近さ + 前フレームの領域からの近さ
            candidate.score = w_circularity * candidate.circularity
                + w_area * candidate.area / max
                + w_center * ARUtil.clothness(centerPt, candidate.center, maxDistance)
                + w_previousFrame * ARUtil.clothness(preCenter, candidate.center, maxDistance);
        }
    }

    public void setLocation(int x, int y)
    {
    }

}