using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;
using System.Linq;

public class RegionCandidateSet {

	public List<RegionCandidate> candidates;

	public RegionCandidateSet (List<MatOfPoint> contours)
	{
		candidates = new List<RegionCandidate> ();
		for (var i = 0; i < contours.Count; i++) {
			candidates.Add (new RegionCandidate (i, contours [i]));
		}
	}

	public RegionCandidateSet (List<RegionCandidate> candidates)
	{
		this.candidates = candidates;
	}

	public RegionCandidateSet elliminateByArea (OpenCVForUnity.Rect searchRect, double minArea, double maxArea) {
		var results = new List<RegionCandidate> ();
		double wholeArea = searchRect.area ();
		foreach (var candidate in this.candidates) {
			double areaRatio = candidate.area / wholeArea;
			if (minArea < areaRatio && areaRatio < maxArea) {
				results.Add (candidate);
			}
		}
		candidates = results;
		return this;
	}

	public RegionCandidateSet elliminateByCircularity (double min) {
		var results = new List<RegionCandidate> ();
		foreach (var candidate in this.candidates) {
			if (candidate.circularity > min) {
				results.Add (candidate);
			}
		}
		candidates = results;
		return this;
	}

    public RegionCandidateSet elliminateByInclusionRect() {
        //var sortedCandidates = candidates.OrderBy(c => c.area).ToList();

        var results = new List<RegionCandidate>();
        for (var i = 0; i < candidates.Count; i++) {
            if (!isContain(i, candidates)) {
                results.Add(candidates[i]);
            }
        }

        candidates = results;
        return this;
    }

    public RegionCandidateSet elliminateByInclusion(OpenCVForUnity.Rect rect) {
        var results = new List<RegionCandidate>();
        foreach (var candidate in candidates)
        {
            if (rect.contains((int)candidate.center.x,(int)candidate.center.y)) {
                results.Add(candidate);
            }
        }

        candidates = results;
        return this;
    }

   bool isContain(int i, List<RegionCandidate> candidates) {
        var center = candidates[i].center;
        for (var j = 0; j < candidates.Count; j++) {
            if (i == j) {
                continue;
            }
            var o = candidates[j];
            if (o.boundingRect.contains((int)center.x, (int)center.y))
            {
                return true;
            }
        }
        return false;
    }

    public RegionCandidateSet score (IScorer scorer) {
		scorer.score (candidates);
		return this;
	}

    public RegionCandidateSet sort() {
		var dst = candidates.OrderBy( c => -c.score);
		this.candidates = dst.ToList();
		return this;
	}

	public RegionCandidate selectTop () {
		if (candidates.Count == 0) {
			return null;
		}
		double maxScore = 0;
		int id = 0;
		for (var i = 0; i < candidates.Count; i++) {
			if (maxScore < candidates [i].score) {
				maxScore = candidates [i].score;
				id = i;
			}
		}
		return candidates [id];
	}

    public RegionCandidate selectWithMaxArea() {
        if (candidates.Count == 0)
        {
            return null;
        }
        var sorted = candidates.OrderBy(c => -c.area).ToList();
        return sorted[0];
    }

    List<Scalar> COLORS = new List<Scalar>
    {
        new Scalar(255, 0, 0),
        new Scalar(0, 255, 0),
        new Scalar(0, 0, 255),
        new Scalar(255, 255, 0),
        new Scalar(0, 255, 255),
        new Scalar(255, 0, 255),
        new Scalar(255, 100, 0),
        new Scalar(0, 100, 255),
    };

    public void drawRects(Mat dst) {
        var count = 0;
        foreach (var candidate in candidates)
        {
            var id = count % COLORS.Count;
            candidate.drawRect(dst, color: COLORS[id]);
            count += 1;
        }
    }
}
