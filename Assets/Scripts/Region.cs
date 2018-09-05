using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;
using System;

public class Region
{
	public Mat parentMat;
	public OpenCVForUnity.Rect rect;
	public Mat mask;
	public Mat roiMask;
	public RegionCandidate candidate;
	public Region previousRegion;
	public Point velocity;
    public string id;

	public Size parentSize
    {
		get { return mask.size (); }
	}

	public Region (int x, int y, int width, int height)
	{
		rect = new OpenCVForUnity.Rect (x, y, width, height);
		candidate = new RegionCandidate ();
        id = createId();
    }

	public Region (RegionCandidate candidate, Mat parentMat)
	{
		this.parentMat = parentMat;
		this.candidate = candidate;

		// 輪郭を塗りつぶしてマスク画像を作成
		mask = Mat.zeros (parentMat.size (), CvType.CV_8UC1);
		Imgproc.drawContours (mask, new List<MatOfPoint>{ candidate.contour }, 0, new Scalar (255), -1);

		//マスク画像のROIを作成
		rect = Imgproc.boundingRect (candidate.contour);
		roiMask = new Mat (mask, rect);
        id = createId();
    }

    string createId() {
        return Guid.NewGuid().ToString("N").Substring(0, 10);
    }

	public Region (RegionCandidate candidate, Mat parentMat, Region previousRegion) : this (candidate, parentMat)
	{
		this.previousRegion = previousRegion;
		this.velocity = new Point (this.center.x - previousRegion.center.x, this.center.y - previousRegion.center.y);
	}

	public Point center
    {
		get {
			return this.candidate.center;
		}
	}

	public double area
    {
		get {
			return candidate.area;
		}
	}

	public double circularity 
    {
		get {
			return candidate.circularity;
		}
	}

	public RotatedRect rotatedRect
    {
		get {
			return candidate.ellipseRotatedRect;
		}
	}

	public OpenCVForUnity.Rect predictNextSearchRect ()
    {
		int margin = Mathf.CeilToInt (rect.width * 0.1f);
		var searchRect = new OpenCVForUnity.Rect (rect.x - margin, rect.y - margin, rect.width + margin * 2, rect.height + margin * 2);
		searchRect = ARUtil.calcRectWithinMat (searchRect, parentMat);
		return searchRect;
	}

    public Region createRefienedRegion(Mat channel, int threshold)
    {
        // もともとのマスクを拡張して、追加のチャンネルで閾値処理をする.

        Mat newMask = Mat.zeros(parentMat.size(), CvType.CV_8U);
        Mat orignalMask = mask.clone();

        Imgproc.dilate(orignalMask, orignalMask, Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(30, 30)));
        Core.add(newMask, orignalMask, newMask);

        Mat c = Mat.zeros(parentMat.size(), CvType.CV_8UC1);
        channel.copyTo(c, newMask);
        Imgproc.threshold(c, c, threshold, 255, Imgproc.THRESH_BINARY);

        var refinedContours = ARUtil.findContours(c);
        var refinedCandidate = new RegionCandidateSet(refinedContours).selectWithMaxArea();
        if (refinedCandidate == null)
        {
            return null;
        }
        return new Region(refinedCandidate, parentMat);
    }

	public void drawRect(Mat toMat, Scalar color=null, int thickness = 1) 
    {
		if (color==null) {
			color = new Scalar (0, 255, 0);
		} 
		Imgproc.rectangle (toMat, rect.tl (), rect.br (), color, thickness);
	}


    static public List<Region> elliminateByInclusionRect(List<Region> regions)
    {
        //var sortedCandidates = candidates.OrderBy(c => c.area).ToList();

        var results = new List<Region>();
        for (var i = 0; i < regions.Count; i++)
        {
            if (!isContain(i, regions))
            {
                results.Add(regions[i]);
            }
        }

        return results;
    }

   static bool isContain(int i, List<Region> regions)
    {
        var center = regions[i].candidate.center;
        for (var j = 0; j < regions.Count; j++)
        {
            if (i == j)
            {
                continue;
            }
            var o = regions[j];
            if (o.candidate.boundingRect.contains((int)center.x, (int)center.y))
            {
                return true;
            }
        }
        return false;
    }
}