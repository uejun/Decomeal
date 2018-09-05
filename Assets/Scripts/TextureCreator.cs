using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;
using System;


public interface TextureCreator {
	Mat create (Region region);
	Mat create(List<Region> regions);
    void alphaBlend(Mat org, Mat texture, Region foodRegion, double alpha);
    void alphaBlend(Mat org, Mat texture, Mat mask, double alpha);
    void changeNext();
}

public class SushiTextureCreator: MonoBehaviour, TextureCreator {
	
	int currentIndex = 0;

	List<Mat> srcList = new List<Mat>{
		Imgcodecs.imread (Utils.getFilePath ("Sushi Images/sarmon.jpg")),
		Imgcodecs.imread (Utils.getFilePath ("Sushi Images/toro.jpg")),
		Imgcodecs.imread (Utils.getFilePath ("Sushi Images/otoro.jpg")),
		Imgcodecs.imread (Utils.getFilePath ("Sushi Images/hamachi.jpg"))
	};

	Mat _mat = Imgcodecs.imread (Utils.getFilePath ("Sushi Images/sarmon.jpg"));

	public SushiTextureCreator() {
		foreach (var mat in this.srcList) {
			Imgproc.cvtColor (mat, mat, Imgproc.COLOR_BGR2RGB);
		}
	}


    bool isConcatenate(Region region, List<Region> regions) {
        var nearDistance = 150;
        foreach (var r in regions) {
            if (region.rect.intersectsWith(r.rect))
            {
                return true;
            }
            if (Math.Abs(region.rect.br().x - r.rect.tl().x) < nearDistance) {
                return true;
            }
            if (Math.Abs(region.rect.br().y - r.rect.tl().y) < nearDistance)
            {
                return true;
            }
            if (Math.Abs(region.rect.tl().x - r.rect.br().x) < nearDistance)
            {
                return true;
            }
            if (Math.Abs(region.rect.tl().y - r.rect.br().y) < nearDistance)
            {
                return true;
            }

        }
        return false;
    }
     
    bool isConcatenateGroup(Region region, Dictionary<string, List<Region>> groups) {
        foreach (KeyValuePair<string, List<Region>> pair in groups)
        {
            if (isConcatenate(region, pair.Value))
            {
                groups[pair.Key].Add(region);
                return true;
            }

        }
        return false;
    } 


	public Mat create(List<Region> regions) {
        if (regions.Count == 0) {
            return null;
        }

        var regionGroups = new Dictionary<string, List<Region>>();
        for (var i = 0; i < regions.Count; i++)
        {
            var exist = isConcatenateGroup(regions[i], regionGroups);
            if (!exist) {
                regionGroups[regions[i].id] = new List<Region> { regions[i] };
            }

        }

        print("regionGroups");
        print(regionGroups.Values.Count);

		Mat resultTexture = Mat.zeros (regions[0].parentSize, CvType.CV_8UC3);

        foreach (KeyValuePair<string, List<Region>> pair in regionGroups)
        {
            if (pair.Value.Count == 1) {
                var tex = createForOne(pair.Value[0]);
                Core.add(resultTexture, tex, resultTexture);
                continue;
            }

            List<Point> points = new List<Point>();
            foreach (var region in pair.Value)
            {
                points.AddRange(region.candidate.contour2f.toList());
            }
            MatOfPoint2f contours = new MatOfPoint2f();
            contours.fromList(points);
            var ellipseRotatedRect = Imgproc.fitEllipse(contours);
            Mat texture = Mat.zeros(regions[0].parentSize, CvType.CV_8UC3);
            Mat originalTexture = srcList[currentIndex];
            ARUtil.affineTransform(originalTexture, texture, ellipseRotatedRect);
            Core.add(resultTexture, texture, resultTexture);

        }

        return coverBlackArea(resultTexture);
	}


    public Mat create (Region region) {
		Mat texture = createForOne(region);
		return coverBlackArea(texture);
	}


	Mat createForOne(Region region) {
		Mat texture = Mat.zeros (region.parentSize, CvType.CV_8UC3);
		Mat originalTexture = srcList[currentIndex];

		if (region.rect.tl().x == 0) {
			var aspect = originalTexture.size().height / originalTexture.size().width;
			var height = (int)region.rect.size().height;
			var width = (int)(height / aspect);
			var y = (int)region.rect.tl().y;
			var x = (int)region.rect.br().x - width;
			var rect = new OpenCVForUnity.Rect(x,y,width,height);
			ARUtil.affineTransform (originalTexture, texture, rect);
		} else if (region.rect.br().x >= region.parentMat.size().width -1) {
			var aspect = originalTexture.size().height / originalTexture.size().width;
			var height = (int)region.rect.size().height;
			var width = (int)(height / aspect);
			var y = (int)region.rect.tl().y;
			var x = (int)region.rect.tl().x;
			var rect = new OpenCVForUnity.Rect(x,y,width,height);
			ARUtil.affineTransform (originalTexture, texture, rect);
		} else {
			// オリジナルのテクスチャ画像を回転矩形にアフィン変換
			ARUtil.affineTransform (originalTexture, texture, region.rotatedRect);
		}
		
		return texture;
	}

	// 斜めになったときに現れる、テクスチャがないことによる黒い領域を防ぐ.
	// 引数のtextureは変更が反映する
	Mat coverBlackArea(Mat texture) {
        Mat originalTexture = srcList[currentIndex];

        // テクスチャのグレー画像作成
        Mat textureGray = new Mat (texture.size(), CvType.CV_8UC1);
		Imgproc.cvtColor (texture, textureGray, Imgproc.COLOR_RGB2GRAY);

		// グレー画像をニ値化 => 黒い領域のみ白に、テクスチャ部分は黒に.
		Mat mask = new Mat (textureGray.size(), CvType.CV_8UC1);
		Imgproc.threshold (textureGray, mask, 0, 255, Imgproc.THRESH_BINARY_INV);

		// RotatedRectの枠線がわずかに残るのを防ぐ
		Imgproc.dilate (mask, mask, Imgproc.getStructuringElement (Imgproc.MORPH_RECT, new Size (3, 3)));
		
		// 背景を大きなテクスチャ画像とする
        Mat background = new Mat (texture.size(), CvType.CV_8UC3);
		Imgproc.resize(originalTexture, background, background.size());

		// 黒い領域は、背景のテクスチャ画像で埋める
		background.copyTo(texture, mask);

		return texture;
	}

	public void alphaBlend(Mat org, Mat texture, Region foodRegion, double alpha) {
		ARUtil.alphaBlend (org, texture, alpha, foodRegion);
	}

	public void alphaBlend(Mat org, Mat texture, Mat mask, double alpha) {
		ARUtil.alphaBlend (org, texture, alpha, mask);
	}

	public void changeNext() {
		if (currentIndex == srcList.Count -1) {
			currentIndex = 0;
		} else {
			currentIndex += 1;
		}
	}

}
