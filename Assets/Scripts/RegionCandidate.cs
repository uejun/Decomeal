using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;

public class RegionCandidate
{
	public int index;
	public MatOfPoint contour;
	public MatOfPoint2f contour2f;
	public double _area;
	public double _circularity;
	public RotatedRect ellipseRotatedRect;
    public OpenCVForUnity.Rect _boundingRect;
	public double score;

    Scalar DEFAULT_DRAW_COLOR = new Scalar(255, 0, 0);

	public RegionCandidate ()
	{
		index = 0;
		contour = new MatOfPoint ();
		contour2f = new MatOfPoint2f ();
		_area = 0.0;
		_circularity = 0.0;
        _boundingRect = null;
	}

	public RegionCandidate (int index, MatOfPoint contour)
	{
		this.index = index;
		this.contour = contour;
		this.contour2f = new MatOfPoint2f (contour.toArray ());
	}


	public Point center {
		get {
            if (ellipseRotatedRect == null) {
                ellipse();
            }
			return ellipseRotatedRect.center;
		}
	}

	public double area {
		get {
			if (_area == 0.0) {
				calcArea ();
			}
			return _area;
		}
    }

	public double circularity {
		get {
			if (_circularity == 0.0) {
				this.calcCircularity ();
			}
			return this._circularity;
		}
	}

    public OpenCVForUnity.Rect boundingRect {
        get {
            if (this._boundingRect == null) {
                this._boundingRect = Imgproc.boundingRect(this.contour);
            }
            return this._boundingRect;
        }
    }

	// 面積を計算する
	public void calcArea ()
	{
		this._area = Imgproc.contourArea (this.contour);
	}

	// 候補輪郭の円形度を計算する
	public void calcCircularity ()
	{
		// 円形度 = 4π * S/L^2 (S = 面積, L = 図形の周囲長)
		double perimeter = Imgproc.arcLength (this.contour2f, true);
		this._circularity = 4.0 * Mathf.PI * this.area / (perimeter * perimeter); // perimeter = 0 のとき気をつける
	}

	public void ellipse ()
	{
		ellipseRotatedRect = Imgproc.fitEllipse (contour2f);
	}

    public void drawRect(Mat dst, Scalar color = null, int thickness = 1)
    {
        if (color==null) {
            color = DEFAULT_DRAW_COLOR;
        }

        Imgproc.rectangle(dst, boundingRect.tl(), boundingRect.br(), color, thickness);
    }

}