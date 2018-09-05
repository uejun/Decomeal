using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using OpenCVForUnity;
using System;

public class TextureManager : MonoBehaviour {

	public Texture2D defaultTexture;

	public string text;

	public Mat textureMat;

	private bool faceLatte = false;

	private ITextureSource _textureSource;

	public ITextureSource textureSource {
		set {
			this._textureSource = value;
		}
		get {
			return this._textureSource;
		}
	}


	void Start ()
	{
		if (defaultTexture != null) 
		{
			textureSource = new ImageSource (defaultTexture);
			textureMat = textureSource.mat;
		} 
		else if (!String.IsNullOrEmpty(text)) 
		{
			string url = "https://3l8h6kvhvg.execute-api.ap-northeast-1.amazonaws.com/dev/text";
			StartCoroutine (getTextImage(url, text));
		} 
		else 
		{
			textureSource = new VideoSource ();
		}
	}
		

	void Update ()
	{
	}
		

	public Mat create (Region region)
	{
		Mat texture = Mat.zeros (region.parentSize, CvType.CV_8UC3);
		if ((defaultTexture != null) || (!String.IsNullOrEmpty (text))) {
			ARUtil.affineTransform (textureSource.mat, texture, region.rect);
		} else {
//			textureMat = createLatteImg (textureSource.mat);
			ARUtil.affineTransform (textureSource.mat, texture, region.rect);
		}
		return texture;
	}

    public Mat create(List<Region> regions)
    {
        Mat resultTexture = Mat.zeros(regions[0].parentSize, CvType.CV_8UC3);
        foreach (var region in regions)
        {
            Mat texture = Mat.zeros(region.parentSize, CvType.CV_8UC3);
            if ((defaultTexture != null) || (!String.IsNullOrEmpty(text)))
            {
                ARUtil.affineTransform(textureSource.mat, texture, region.rect);
            }
            else
            {
                //          textureMat = createLatteImg (textureSource.mat);
                ARUtil.affineTransform(textureSource.mat, texture, region.rect);
            }
            Core.add(resultTexture, texture, resultTexture);
        }
        
        return coverBlackArea(resultTexture, textureSource.mat);
    }

    Mat coverBlackArea(Mat texture, Mat originalTexture)
    {
      
        // テクスチャのグレー画像作成
        Mat textureGray = new Mat(texture.size(), CvType.CV_8UC1);
        Imgproc.cvtColor(texture, textureGray, Imgproc.COLOR_RGB2GRAY);

        // グレー画像をニ値化 => 黒い領域のみ白に、テクスチャ部分は黒に.
        Mat mask = new Mat(textureGray.size(), CvType.CV_8UC1);
        Imgproc.threshold(textureGray, mask, 0, 255, Imgproc.THRESH_BINARY_INV);

        // RotatedRectの枠線がわずかに残るのを防ぐ
        Imgproc.dilate(mask, mask, Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(3, 3)));

        // 背景を大きなテクスチャ画像とする
        Mat background = new Mat(texture.size(), CvType.CV_8UC3);
        Imgproc.resize(originalTexture, background, background.size());

        // 黒い領域は、背景のテクスチャ画像で埋める
        background.copyTo(texture, mask);

        return texture;
    }



    IEnumerator getTextImage (string url, string text)
	{
		WWWForm form = new WWWForm();
		form.AddField("text", text);
		WWW request = new WWW(url, form);
		while (!request.isDone) {
			yield return null;
		}
		defaultTexture = request.texture;
		textureSource = new ImageSource (defaultTexture);
	}


	public Mat createLatteImg (Mat src)
	{
		Mat returnMat = new Mat(src.rows(), src.cols(), CvType.CV_8UC3);
		Mat backMat = Imgcodecs.imread (Utils.getFilePath ("back_coffee.jpg"));
		Imgproc.resize(backMat, backMat, new Size(src.cols(), src.rows()));
		Mat multiThreshMat = new Mat (src.rows (), src.cols(), CvType.CV_8UC3);
		int seperation = 3;
		List<int> rValueList = splitValue (107, 196, seperation);
		List<int> gValueList = splitValue (24, 176, seperation);
		List<int> bValueList = splitValue (11, 161, seperation);
		multiThreshMat = multiValueThresholdRGB (src, rValueList, gValueList, bValueList, seperation);
		src = convertLatte (multiThreshMat, backMat);
		Imgproc.cvtColor (src, returnMat, Imgproc.COLOR_BGR2RGB);
		return returnMat;
	}


	public Mat multiValueThresholdRGB(Mat img, List<int> rValueList, List<int> gValueList, List<int>bValueList, int seperation)
	{
		Mat imgGray = new Mat(img.rows(), img.cols(), CvType.CV_8UC1);
		Imgproc.cvtColor(img, imgGray, Imgproc.COLOR_BGR2GRAY);
		Core.MinMaxLocResult minMaxResult = Core.minMaxLoc(imgGray);
		int threshValue = (int)minMaxResult.minVal;
		int threshValueInterval = (int)((minMaxResult.maxVal - minMaxResult.minVal) / seperation);
		Mat multiThreshMat = new Mat (img.rows(), img.cols(), CvType.CV_8UC3);
		Mat multiThreshStageMat = new Mat (img.rows(), img.cols(), CvType.CV_8UC3);
		Mat colorStageMat = new Mat (img.rows(), img.cols(), CvType.CV_8UC3);
		Mat threshMask = new Mat (img.rows(), img.cols(), CvType.CV_8UC1);

		colorStageMat = createMonochromaticRGB(rValueList[(seperation - 2)],
			gValueList[(seperation - 2)], 
			bValueList[(seperation - 2)],
			img.cols(), img.rows());
	
		for(int i = 0;i < seperation; i++){
			colorStageMat = createMonochromaticRGB(rValueList[(seperation - i - 1)],
				gValueList[(seperation - i - 1)], 
				bValueList[(seperation - i - 1)],
				img.cols(), img.rows());

			if(i < seperation - 1){
				if (i == 0) {
					colorStageMat = create_form (colorStageMat);
				}
				threshValue += threshValueInterval;
				threshMask = rangeThreshold(imgGray, threshValue - threshValueInterval, threshValue, 255);
				Core.bitwise_and(colorStageMat, colorStageMat, multiThreshStageMat, threshMask);
				Core.bitwise_or(multiThreshMat, multiThreshStageMat, multiThreshMat);
			}else if(i == seperation - 1){
				Imgproc.threshold(imgGray, threshMask, threshValue, 255, Imgproc.THRESH_BINARY);
				Core.bitwise_and(colorStageMat, colorStageMat, multiThreshStageMat, threshMask);
				Core.bitwise_or(multiThreshMat, multiThreshStageMat, multiThreshMat);
			}
		}
		return multiThreshMat;
	}

	public List<int> splitValue(int maxValue, int minValue, int seperation)
	{
		int interval = (int)((maxValue - minValue) / (seperation - 1));
		List<int> valueList = new List<int>(){};
		for(int i = 0; i < seperation; i++){
			valueList.Add(minValue);
			minValue += interval;
		}
		return valueList;
	}


	public Mat convertLatte(Mat multiMat, Mat backMat)
	{
		Mat latteMat = new Mat (multiMat.rows(), multiMat.cols(), CvType.CV_8UC3);
		Imgproc.GaussianBlur(multiMat, multiMat, new Size(5, 5), 0);
		Core.addWeighted (multiMat, 0.5, backMat, 0.5, 0.0, latteMat);
		return latteMat;
	}


	public Mat multiValueThresholdHSV(Mat img, List<int> hValueList, List<int> sValueList, List<int>vValueList, int seperation)
	{
		int imgWidth = img.cols();
		int imgHeight = img.rows();
		Mat imgGray = new Mat(imgHeight, imgWidth, CvType.CV_8UC1);
		Imgproc.cvtColor(img, imgGray, Imgproc.COLOR_BGR2GRAY);
		Core.MinMaxLocResult minMaxResult = Core.minMaxLoc(imgGray);
		int minGrayValue = (int)minMaxResult.minVal;
		int maxGrayValue = (int)minMaxResult.maxVal;
		int threshValue = minGrayValue;
		int threshValueInterval = (int)((maxGrayValue - minGrayValue) / seperation);
		Mat multiThreshMat = new Mat (imgHeight, imgWidth, CvType.CV_8UC3);
		Mat multiThreshStageMat = new Mat (imgHeight, imgWidth, CvType.CV_8UC3);
		Mat colorStageMat = new Mat (imgHeight, imgWidth, CvType.CV_8UC3);
		Mat threshMask = new Mat (imgHeight, imgWidth, CvType.CV_8UC1);
		colorStageMat = createMonochromaticHSV2RGB(hValueList[(seperation - 2)],
			sValueList[(seperation - 2)], 
			vValueList[(seperation - 2)],
			imgWidth, imgHeight);

		for(int i = 0;i < seperation; i++){
			colorStageMat = createMonochromaticRGB(hValueList[(seperation - i - 1)],
				sValueList[(seperation - i - 1)], 
				vValueList[(seperation - i - 1)],
				imgWidth, imgHeight);

			if(i < seperation - 1){
				threshValue += threshValueInterval;
				threshMask = rangeThreshold(imgGray, threshValue - threshValueInterval, threshValue, 255);
				Core.bitwise_and(colorStageMat, colorStageMat, multiThreshStageMat, threshMask);
				Core.bitwise_or(multiThreshMat, multiThreshStageMat, multiThreshMat);
			}else if(i == seperation - 1){
				Imgproc.threshold(imgGray, threshMask, threshValue, 255, Imgproc.THRESH_BINARY);
				Core.bitwise_and(colorStageMat, colorStageMat, multiThreshStageMat, threshMask);
				Core.bitwise_or(multiThreshMat, multiThreshStageMat, multiThreshMat);
			}
		}
		return multiThreshMat;
	}


	public Mat create_form(Mat latteMat)
	{
		int numOfForm = 800;
		int maxRadius = 3;
		Scalar color = new Scalar (0, 30, 84);
		for (int i = 0; i < numOfForm; i++) {
			int xRandom = (int)(UnityEngine.Random.value * latteMat.cols ());
			int yRandom = (int)(UnityEngine.Random.value * latteMat.rows ());
			int radiusRandom = (int)(UnityEngine.Random.value * maxRadius);
			Imgproc.circle (latteMat, new Point (xRandom, yRandom), radiusRandom, color, -1);
		}
		return latteMat;
	}


	public Mat rangeThreshold(Mat img, int minThesh, int maxThresh, int maxVal)
	{
		Mat minThreshMask = new Mat(img.rows(), img.cols(), CvType.CV_8UC3);
		Mat maxThreshMask = new Mat(img.rows(), img.cols(), CvType.CV_8UC3);
		Mat threshMask = new Mat(img.rows(), img.cols(), CvType.CV_8UC3);
		Imgproc.threshold(img, minThreshMask, minThesh, maxVal, Imgproc.THRESH_BINARY);
		Imgproc.threshold(img, maxThreshMask, maxThresh, maxVal, Imgproc.THRESH_BINARY_INV);
		Core.bitwise_and(minThreshMask, maxThreshMask, threshMask);
		return threshMask;
	}


	public Mat createMonochromaticHSV2RGB(int h, int s, int v, int width, int height)
	{
		Mat rgbMat = new Mat(height, width, CvType.CV_8UC3);
		Mat hsvMat = new Mat(new Size(width, height), CvType.CV_8UC3, new Scalar(h, s, v));
		Imgproc.cvtColor(hsvMat, rgbMat, Imgproc.COLOR_HSV2BGR);
		return rgbMat;
	}


	public Mat createMonochromaticRGB(int r, int g, int b, int width, int height){
		Mat rgbMat = new Mat (height, width, CvType.CV_8UC3, new Scalar (b, g, r));
		return rgbMat;
	}
}
