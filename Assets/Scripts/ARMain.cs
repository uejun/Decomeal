using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;
using OpenCVForUnity;

// このクラスは食のARのメイン処理の流れが書かれている
// ARCamera内のCameraオブジェクトにAddComponentしてください.
public class ARMain : MonoBehaviour {

	// テクスチャ変換のオンオフのためのフラグ
	public static bool willChange = true;
		
	// 入力用
	Texture2D cameraTexture; // UnityEngineから取得する画像
	Mat cameraMat; // OpenCVの入力画像. cameraTextureから毎フレームはじめに変換して得る

	// 食品領域に貼りつけるテクスチャ. コーヒーならカフェオレの画像など.
	Mat texture;

	// 出力用
	public GameObject outputCamera1; // 変換後の画像を撮影するためのカメラ. インスペクタで設定してください
	OutputCamQuad camQuad1; // outputCamera1が撮影するQuad. 変換後の画像を投影する

    
	public double threshold = 0.0;
    public double alpha = 0.5;

	// ARCamera内のCameraオブジェクトにAddComponentされていること.
	TextureManager textureManager;

	// 前回のコーヒー領域を保持
	Region coffeeRegion;
	Cup cup;

	public string TargetObjectName = "CupCylinderTarget";

	void Start ()
	{
		// 入力画像用
		cameraMat = new Mat (Screen.height, Screen.width, CvType.CV_8UC3);
		cameraTexture = new Texture2D (cameraMat.cols (), cameraMat.rows (), TextureFormat.ARGB32, false);

		// 出力画像用
		camQuad1 = outputCamera1.GetComponent<OutputCamQuad> ();

		// コップの初期化
		var cupTargetBehaviour = GameObject.Find (TargetObjectName).GetComponent<CylinderTargetBehaviour> ();
		cup = new Cup (cupTargetBehaviour, cameraMat.rows ());

		// コーヒー領域の初期化
		coffeeRegion = new Region (0, 0, cameraMat.cols (), cameraMat.rows ());

		// テクスチャマネージャーの初期化
		textureManager = GameObject.Find ("ARCamera").GetComponent<TextureManager> ();

	}

	// Update is called once per frame
	void OnPostRender ()
	{
		// UnityのTexture2DからOpencvのMatに変換
		UnityEngine.Rect wholeRect = new UnityEngine.Rect (0, 0, cameraTexture.width, cameraTexture.height);
		cameraTexture.ReadPixels (wholeRect, 0, 0, true);
		Utils.texture2DToMat (cameraTexture, cameraMat);

		/* 初期化 */
		IScorer scorer = null; // 領域判定のスコアラー
		OpenCVForUnity.Rect searchRect = null; // 探索領域矩形
		double v_threshold = 0.0; // 輪郭探索するための二値画像を作るときの明るさの閾値

		// Vuforiaでのコップ検出可否によって、以下を切り替える
		// ・探索対象領域
		// ・スコアー戦略
		// ・二値化の閾値
		if (cup.isNotFound ()) {
			// TODO: 高速化のため、対象物の探索領域を前回領域に限定する
			//if (coffeeRegion.circularity > 0.50) {
			//	searchRect = coffeeRegion.predictNextSearchRect ();
			//	searchRect = ARUtil.calcRectWithinMat (searchRect, cameraMat);
			//} else {
			//	searchRect = new OpenCVForUnity.Rect (0, 0, cameraMat.cols (), cameraMat.rows ());
			//}
            searchRect = new OpenCVForUnity.Rect(0, 0, cameraMat.cols(), cameraMat.rows());
            scorer = new CoffeeScorer (searchRect, coffeeRegion.rect);
			v_threshold = 50.0;
		} else if (cup.isTracked ()) {
			print ("Cup is tracked.");
			cup.update ();
			if (cup.culminationAltitude () < 0.5) {
				goto show;
			}
			// カップの上面の矩形を探索対象矩形とする. 
			searchRect = cup.getTopSurfaceRect (cameraMat);
			scorer = new CoffeeWithCupScorer (searchRect);
			v_threshold = 80.0;
		}


		try {

			// 入力画像から探索領域のROI画像を作成
			Mat roiMat = new Mat (cameraMat, searchRect);

			// HSVチャンネル作成. Vチャンネルで二値化するため
			var hsvChannels = ARUtil.getHSVChannels (roiMat);

			// 上で決めたVチャンネルの閾値で二値化することによって、輪郭探索の準備をする
			// コーヒー表面候補領域と二値画像を作成. すでにROIが絞られているので, 閾値は緩め.
			Mat binaryROIMat = new Mat (roiMat.size (), CvType.CV_8UC1);
			Imgproc.threshold (hsvChannels [2], binaryROIMat, v_threshold, 255, Imgproc.THRESH_BINARY_INV);
			// Imgproc.adaptiveThreshold (hsvChannels [2], coffeeBinaryMat, 255, Imgproc.ADAPTIVE_THRESH_MEAN_C, Imgproc.THRESH_BINARY_INV, 5, 5);

			// 膨張させて輪郭抽出を微修正
			Imgproc.dilate (hsvChannels [2], hsvChannels [2], Imgproc.getStructuringElement (Imgproc.MORPH_RECT, new Size (5, 5)));

			// 二値画像&探索領域矩形で輪郭探索
			var contours = ARUtil.findContours (binaryROIMat, searchRect.tl ());

            // コーヒー領域候補群を作成 -> 円形度で除外 -> 候補にスコアをつける -> 最大スコアの候補を取得
            var regionSet = new RegionCandidateSet(contours)
                .elliminateByArea(searchRect, 0.01, 0.9)
                .elliminateByCircularity(0.2)
                .score(scorer)
                .sort();

            var count = 0;
            var regions = new List<Region>();
            foreach (var candidate in regionSet.candidates)
            {
                if (count > 5) {
                    break;
                }
                if (candidate == null)
                {
                    print("candite is null");
                    break;
                   
                }
                // 領域作成
                coffeeRegion = new Region(candidate, cameraMat);
                regions.Add(coffeeRegion);
                count++;
            }


			// テクスチャ作成
			texture = textureManager.create (regions);

            Mat alphaMask = Mat.zeros(cameraMat.size(), CvType.CV_8U);
            foreach (var region in regions)
            {
                Core.add(region.mask, alphaMask, alphaMask);
            }

            if (willChange)
            {
                // アルファブレンド
                ARUtil.alphaBlend(cameraMat, texture, alpha, alphaMask);
            }

            //Debug.Log (candidate.circularity);
            // 矩形描画
            //Imgproc.rectangle (cameraMat, coffeeRegion.rect.tl (), coffeeRegion.rect.br (), new Scalar (0, 255, 0), 3);

		} catch (System.Exception e){
            print(e);
			goto show;
		}

    show:
		camQuad1.setMat (cameraMat);
	}
}
