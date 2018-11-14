using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;
using System;


public class SushiTextureCreator: MonoBehaviour {
	
    Mat mat_Otoro;
    Mat mat_Tyutoro;
    Mat mat_Sarmon;
    Mat mat_Hamachi;

	public SushiTextureCreator() {
        mat_Sarmon = Imgcodecs.imread(Utils.getFilePath("Sushi Images/sarmon.jpg"));
        mat_Tyutoro = Imgcodecs.imread(Utils.getFilePath("Sushi Images/toro.jpg"));
        mat_Otoro = Imgcodecs.imread(Utils.getFilePath("Sushi Images/otoro.jpg"));
        mat_Hamachi = Imgcodecs.imread(Utils.getFilePath("Sushi Images/hamachi.jpg"));

        //Mat mat_Sarmon = new Mat(_m1.size(), CvType.CV_8UC3);
        //Mat mat_Tyutoro = new Mat(_m2.size(), CvType.CV_8UC3);
        //Mat mat_Otoro = new Mat(m3.size(), CvType.CV_8UC3);
        //Mat mat_Hamachi = new Mat(m4.size(), CvType.CV_8UC3);

        Imgproc.cvtColor(mat_Sarmon, mat_Sarmon, Imgproc.COLOR_BGR2RGB);
        Imgproc.cvtColor(mat_Tyutoro, mat_Tyutoro, Imgproc.COLOR_BGR2RGB);
        Imgproc.cvtColor(mat_Otoro, mat_Otoro, Imgproc.COLOR_BGR2RGB);
        Imgproc.cvtColor(mat_Hamachi, mat_Hamachi, Imgproc.COLOR_BGR2RGB);

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


	public Mat create(Mat cameraMat, List<Region> regions) {
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

        Mat resultTexture = Mat.zeros (regions[0].parentSize, cameraMat.type());

        foreach (KeyValuePair<string, List<Region>> pair in regionGroups)
        {
            // 領域グループ内の領域が一つの場合
            if (pair.Value.Count == 1) {
                var tex = createForOne(cameraMat, pair.Value[0]);
                Core.add(resultTexture, tex, resultTexture);
                continue;
            }

            // 領域グループ内の領域の輪郭点を全て一つのリストにまとめる
            List<Point> points = new List<Point>();
            foreach (var region in pair.Value)
            {
                points.AddRange(region.candidate.contour2f.toList());
            }

            // グループの輪郭作成
            MatOfPoint2f contours = new MatOfPoint2f();
            contours.fromList(points);

            // グループの輪郭のRotatedRectを得る
            var ellipseRotatedRect = Imgproc.fitEllipse(contours);

            // 事前用意してあるテクスチャ画像を食品領域のRotatedRectに射影変換
            Mat originalTexture = selectOriginalTextureImage();

            Mat texture = Mat.zeros(regions[0].parentSize, cameraMat.type());
            ARUtil.affineTransform(originalTexture, texture, ellipseRotatedRect);

            Core.add(resultTexture, texture, resultTexture);

        }

        return coverBlackArea(resultTexture);
	}

    private Mat selectOriginalTextureImage() {
        switch(SushiManager.sushiTextureType) {
            case SushiTextureType.otoro:
                return mat_Otoro;
              
            case SushiTextureType.tyutoro:
                return mat_Tyutoro;
               
            case SushiTextureType.sarmon:
                return mat_Sarmon;
               
            case SushiTextureType.hamachi:
                return mat_Hamachi;
            default:
                return mat_Otoro;

        }
    }


	Mat createForOne(Mat cameraMat, Region region) {
		Mat texture = Mat.zeros (region.parentSize, cameraMat.type());

        Mat originalTexture = selectOriginalTextureImage();

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
        Mat originalTexture = selectOriginalTextureImage();

        // テクスチャのグレー画像作成
        Mat textureGray = new Mat (texture.size(), CvType.CV_8UC1);
		Imgproc.cvtColor (texture, textureGray, Imgproc.COLOR_RGB2GRAY);

		// グレー画像をニ値化 => 黒い領域のみ白に、テクスチャ部分は黒に.
		Mat mask = new Mat (textureGray.size(), CvType.CV_8UC1);
		Imgproc.threshold (textureGray, mask, 0, 255, Imgproc.THRESH_BINARY_INV);

		// RotatedRectの枠線がわずかに残るのを防ぐ
		Imgproc.dilate (mask, mask, Imgproc.getStructuringElement (Imgproc.MORPH_RECT, new Size (3, 3)));
		
		// 背景を大きなテクスチャ画像とする
        Mat background = new Mat (texture.size(), texture.type());
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



}

public class ColorTextureCreator {
    int targetH;
    int targetS;
    int targetV;
    double alpha;
    
    public ColorTextureCreator(int h, int s, int v, double alpha) {
        targetH = h;
        targetS = s;
        targetV = v;
        this.alpha = alpha;
    }

    public Mat create(Mat srcMat, Mat mask, int srcMeanH, int srcMeanS, int srcMeanV) {
        // 最終のテクスチャ画像
        Mat texture = Mat.zeros(srcMat.size(), srcMat.type());

        // srcMatを元のテクスチャとする.
        //Mat orgTexture = Mat.zeros(srcMat.size(), CvType.CV_8UC3);
        //srcMat.copyTo(orgTexture, mask);

        // 色を変換するのでHSVチャンネルを取得
        var hsvChannels = ARUtil.getHSVChannels(srcMat);

        Mat H_Texture = Mat.zeros(srcMat.size(), CvType.CV_8UC1);
        //Mat S_Texture = Mat.zeros(srcMat.size(), CvType.CV_8UC1);
        //Mat V_Texture = Mat.zeros(srcMat.size(), CvType.CV_8UC1);

        var H_beta = targetH - srcMeanH;

        // HSVをそれぞれ変換
        hsvChannels[0].convertTo(H_Texture, H_Texture.type(), alpha: 1.0, beta: H_beta);
        //hsvChannels[1].convertTo(S_Texture, S_Texture.type(), alpha: 0.0, beta: 0.0);
        //hsvChannels[2].convertTo(V_Texture, V_Texture.type(), alpha: 1.0, beta: 0.0);

        // マージしてRGBに戻す
        Core.merge(new List<Mat> { H_Texture, hsvChannels[1], hsvChannels[2] }, texture);
        //Core.merge(new List<Mat> {H_Texture, S_Texture, V_Texture}, texture);
        Imgproc.cvtColor(texture, texture, Imgproc.COLOR_HSV2RGB);

        return texture;
    }

    public void alphaBlend(Mat org, Mat texture, Region foodRegion, double alpha)
    {
        ARUtil.alphaBlend(org, texture, alpha, foodRegion);
    }

    public void alphaBlend(Mat org, Mat texture, Mat mask, double alpha)
    {
        ARUtil.alphaBlend(org, texture, alpha, mask);
    }
}


public class OrangeTextureCreator
{

    public OrangeTextureCreator()
    {
    
    }

    public Mat create(Mat srcMat, Mat mask)
    {
        Mat texture = Mat.zeros(srcMat.size(), srcMat.type());
        Mat orgTexture = Mat.zeros(srcMat.size(), srcMat.type());

        srcMat.copyTo(orgTexture, mask);

        var hsvChannels = ARUtil.getHSVChannels(orgTexture);

        //Mat targetSchan = Mat.zeros(srcMat.size(), CvType.CV_8UC1);
        //hsvChannels[1].convertTo(targetSchan, targetSchan.type(), alpha: 0.5, beta: 0.0);

        //Core.merge(new List<Mat> { hsvChannels[0], targetSchan, hsvChannels[2] }, texture);

        Mat targetSchan = Mat.zeros(srcMat.size(), CvType.CV_8UC1);
        hsvChannels[0].convertTo(targetSchan, targetSchan.type(), alpha: 1.0, beta: -20.0);

        Core.merge(new List<Mat> { targetSchan, hsvChannels[1], hsvChannels[2] }, texture);

        Imgproc.cvtColor(texture, texture, Imgproc.COLOR_HSV2RGB);

        return texture;

    }


    public void alphaBlend(Mat org, Mat texture, Region foodRegion, double alpha)
    {
        ARUtil.alphaBlend(org, texture, alpha, foodRegion);
    }

    public void alphaBlend(Mat org, Mat texture, Mat mask, double alpha)
    {
        ARUtil.alphaBlend(org, texture, alpha, mask);
    }

}

public class NoodleTextureCreator
{
    int currentIndex = 0;

    Mat mat_Hot;
    Mat mat_Creamy;
    Mat mat_Thick;


    public NoodleTextureCreator()
    {
        Mat _mat_Hot = Imgcodecs.imread(Utils.getFilePath("Noodle Images/hot-spice.jpg"));
        Mat _mat_Creamy = Imgcodecs.imread(Utils.getFilePath("Noodle Images/creamy.png"));
        Mat _mat_Thick = Imgcodecs.imread(Utils.getFilePath("Noodle Images/thick2.jpg"));

        mat_Hot = new Mat(_mat_Hot.size(), CvType.CV_8UC3);
        mat_Creamy = new Mat(_mat_Creamy.size(), CvType.CV_8UC3);
        mat_Thick = new Mat(_mat_Thick.size(), CvType.CV_8UC3);

        Imgproc.cvtColor(_mat_Hot, mat_Hot, Imgproc.COLOR_BGR2RGB);
        Imgproc.cvtColor(_mat_Creamy, mat_Creamy, Imgproc.COLOR_BGRA2RGB);
        Imgproc.cvtColor(_mat_Thick, mat_Thick, Imgproc.COLOR_BGR2RGB);
        
    }

    public Mat create(Mat srcMat, Mat mask)
    {
        // 最終のテクスチャ画像
        Mat resultTexture = Mat.zeros(srcMat.size(), CvType.CV_8UC3);

        if (NoodleManager.noodleTextureType == NoodleTextureType.hot)
        {
            Mat textureMat = new Mat(srcMat.size(), CvType.CV_8UC3);
            Imgproc.resize(mat_Hot, textureMat, srcMat.size());
            textureMat.copyTo(resultTexture, mask);
        }
        else if (NoodleManager.noodleTextureType == NoodleTextureType.creamy)
        {
            Mat textureMat = new Mat(srcMat.size(), CvType.CV_8UC3);
            Imgproc.resize(mat_Creamy, textureMat, srcMat.size());
            textureMat.copyTo(resultTexture, mask);
        }
        else if (NoodleManager.noodleTextureType == NoodleTextureType.thick)
        {
            Mat textureMat = new Mat(srcMat.size(), CvType.CV_8UC3);
            Imgproc.resize(mat_Thick, textureMat, srcMat.size());
            textureMat.copyTo(resultTexture, mask);
        }
        else
        {
            // 色を変換するのでHSVチャンネルを取得
            var hsvChannels = ARUtil.getHSVChannels(srcMat);

            //Mat H_Texture = Mat.zeros(srcMat.size(), CvType.CV_8UC1);
            Mat S_Texture = Mat.zeros(srcMat.size(), CvType.CV_8UC1);
            Mat V_Texture = Mat.zeros(srcMat.size(), CvType.CV_8UC1);

            // HSVをそれぞれ変換
            var S_alpha = 1.0;
            var V_alpha = 1.0;
            if (NoodleManager.noodleTextureType == NoodleTextureType.thick)
            {
                S_alpha = 0.5;
                V_alpha = 0.25;
            }
            else
            {
                S_alpha = 0.3;
                V_alpha = 1.0;
            }
            //hsvChannels[0].convertTo(H_Texture, H_Texture.type(), alpha: 1.0, beta: H_beta);
            hsvChannels[1].convertTo(S_Texture, S_Texture.type(), alpha: S_alpha, beta: 0.0);
            hsvChannels[2].convertTo(V_Texture, V_Texture.type(), alpha: V_alpha, beta: 0.0);

            // マージしてRGBに戻す
            Core.merge(new List<Mat> { hsvChannels[0], S_Texture, V_Texture }, resultTexture);

            Imgproc.cvtColor(resultTexture, resultTexture, Imgproc.COLOR_HSV2RGB);
        }

        return resultTexture;
    }


    public void alphaBlend(Mat org, Mat texture, Region foodRegion, double alpha)
    {
        ARUtil.alphaBlend(org, texture, alpha, foodRegion);
    }

    public void alphaBlend(Mat org, Mat texture, Mat mask, double alpha)
    {
        ARUtil.alphaBlend(org, texture, alpha, mask);
    }

}


