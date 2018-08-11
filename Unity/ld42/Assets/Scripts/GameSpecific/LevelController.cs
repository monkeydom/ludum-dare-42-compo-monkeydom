using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace MonkeydomSpecific {

	public class LevelController : MonoBehaviour {

		[Header("Outlets")]
		public GameObject segmentPrefab;
		public Material segmentBaseColor;

		public GameObject segmentsContainer;

		List<SegmentData> segments;

		List<Material> segmentColor;

		int width = 35;

		// Use this for initialization
		public void Start() {
			InitializeSegments();
			GenerateSegmentObjects();
		}

		void InitializeSegments() {
			segments = new List<SegmentData>();
			int location = 0;
			int fileCount = Random.Range(3, 10);
			for (int index = 0; index < fileCount; index++) {
				int segmentCount = Random.Range(3, 10);
				for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++) {
					SegmentData data = new SegmentData();
					data.fileNumber = index;
					data.segmentNumber = segmentIndex + 1;
					data.segmentLength = Random.Range(1, 8);
					data.location = location;
					location += data.segmentLength + 1 + Random.Range(0, 4);
					segments.Add(data);
				}
			}

			GenerateColors(fileCount);
		}

		void GenerateColors(int upTo) {
			segmentColor = Enumerable.Range(1, upTo).Select(x => {
				Material mat = Instantiate(segmentBaseColor);
				Color color = mat.color;
				float h, s, v;
				Color.RGBToHSV(color, out h, out s, out v);
				h = Random.Range(0, 1.0f);
				mat.color = Color.HSVToRGB(h, s, v);
				return mat;
			}).ToList<Material>();
		}

		Vector3 PositionForLocation(int location) {
			int x = location % width;
			int y = location / width;
			var position = new Vector3(x, -y, 0);
			return position;
		}

		void GenerateSegmentObjects() {
			if (segmentsContainer) {
				Destroy(segmentsContainer);
			}
			segmentsContainer = new GameObject("Segments Container");

			Transform transform = segmentsContainer.transform;
			transform.localPosition = new Vector3(-width / 2.0f, 13, 0.1f);
			transform.parent = gameObject.transform;

			foreach (SegmentData segment in segments) {
				GameObject segmentObject = Instantiate<GameObject>(segmentPrefab, transform);
				var sb = segmentObject.GetComponent<SegmentBehavior>();
				sb.SetSegmentData(segment);
				segmentObject.transform.localPosition = PositionForLocation(segment.location);
				foreach (MeshRenderer renderer in segmentObject.GetComponentsInChildren<MeshRenderer>()) {
					if (renderer.gameObject.tag == "SegmentObject") {
						renderer.sharedMaterial = segmentColor[segment.fileNumber];
					}
				}
			}
		}

		// Update is called once per frame
		void Update() {

		}
	}

}