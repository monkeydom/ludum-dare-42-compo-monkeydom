using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace MonkeydomSpecific {

	public class LevelController : MonoBehaviour {

		SegmentBehavior hoverSegment;
		SegmentBehavior selectedSegment;

		GameObject temporaryMoveSegment;

		[Header("Outlets")]
		public Camera mainCamera;
		public GameObject segmentPrefab;
		public Material segmentBaseColor;
		public Material[] segmentColorPalette;
		public GameObject segmentsContainer;
		List<SegmentData> segments;

		List<Material> segmentColor;

		int width = 35;

		// Use this for initialization
		public void Start() {
			InitializeSegments();
			GenerateSegmentObjects();
			GameController.Instance.DebugOutput($"Segments: {segments.Count()}\nFiles: {segments.Last().fileNumber}");
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
			int start = Random.Range(0, 18);
			segmentColor = Enumerable.Range(1, upTo).Select(x => {
				if (segmentColorPalette.Count() > 0) {
					return segmentColorPalette[(x + start) % segmentColorPalette.Count()];
				} else {
					Material mat = Instantiate(segmentBaseColor);
					Color color = mat.color;
					float h, s, v;
					Color.RGBToHSV(color, out h, out s, out v);
					h = Random.Range(0, 1.0f);
					mat.color = Color.HSVToRGB(h, s, v);
					return mat;
				}
			}).ToList<Material>();
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
				GameObject segmentObject = Instantiate(segmentPrefab, transform);
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

		#region Geometry stuff

		Vector3 PositionForLocation(int location) {
			int x = location % width;
			int y = location / width;
			var position = new Vector3(x, -y, 0);
			return position;
		}

		#endregion


		void Update() {
			HandleInput();
		}

		void HandleInput() {
			if (Input.GetButtonDown("Jump")) {
				FindObjectOfType<LevelController>().Start();
			}

			Vector3 mousePosition = Input.mousePosition;
			Ray ray = mainCamera.ScreenPointToRay(mousePosition);
			RaycastHit hit;
			SegmentBehavior segmentUnderMouse = null;
			if (Physics.Raycast(ray, out hit, Mathf.Infinity, GameController.LayerMaskSegments)) {
				Transform objectHit = hit.transform;
				segmentUnderMouse = objectHit.parent.GetComponentInParent<SegmentBehavior>();
			}

			if (!selectedSegment) {
				SetHoverSegment(segmentUnderMouse);
			} else {
				if (segmentUnderMouse) {
					temporaryMoveSegment.SetActive(false);
				} else {
					Plane plane = new Plane(Vector3.forward, 0);
					float enter = 0.0f;

					if (plane.Raycast(ray, out enter)) {
						//Get the point that is clicked
						Vector3 hitPoint = ray.GetPoint(enter);

						//Move your cube GameObject to the point where you clicked
						temporaryMoveSegment.transform.position = hitPoint;
						temporaryMoveSegment.SetActive(true);
					}
				}
			}

			if (Input.GetButtonDown("Fire1")) {
				if (hoverSegment) {
					SetSelectedSegment(hoverSegment);
				} else if (selectedSegment) {
					SetSelectedSegment(null);
				}
			}
		}

		void SetHoverSegment(SegmentBehavior segment) {
			if (hoverSegment != segment) {
				if (hoverSegment) {
					hoverSegment.highlighted = false;
				}
				if (segment) {
					segment.highlighted = true;
				}
				GameController.Instance.DebugOutput($"Hover change from {hoverSegment} to {segment}");
				hoverSegment = segment;
			}
		}

		void SetSelectedSegment(SegmentBehavior segment) {
			if (selectedSegment != segment) {
				if (selectedSegment) {
					selectedSegment.selected = false;
				}
			}
			selectedSegment = segment;
			GameController.Instance.DebugOutput($"Selection change from {selectedSegment} to {segment}");
			if (segment) {
				SetHoverSegment(null);
				segment.selected = true;

				temporaryMoveSegment = Instantiate(selectedSegment.gameObject, selectedSegment.transform.parent);
				temporaryMoveSegment.transform.localPosition = temporaryMoveSegment.transform.localPosition - (Vector3.back * 0.5f);
				temporaryMoveSegment.SetActive(false);
				foreach (Collider col in temporaryMoveSegment.GetComponentsInChildren<Collider>()) {
					col.enabled = false;
				}
				temporaryMoveSegment.name = "temporaryMoveSegment";
			} else {
				if (temporaryMoveSegment) {
					Destroy(temporaryMoveSegment);
				}
			}
		}
	}

}