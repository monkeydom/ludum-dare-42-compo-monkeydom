using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace MonkeydomSpecific {

	public class LevelController : MonoBehaviour {

		SegmentBehavior hoverSegment;
		SegmentBehavior selectedSegment;

		GameObject temporaryMoveSegment;

		Level level;

		[Header("Outlets")]
		public Camera mainCamera;
		public GameObject segmentPrefab;
		public Material segmentBaseColor;
		public Material[] segmentColorPalette;
		public GameObject segmentsContainer;

		List<Material> segmentColor;

		// Use this for initialization
		public void Start() {
			level = new Level(34, 34 * 20 + 13, Random.Range(3, 5), Random.Range(16, 60));
			//			InitializeSegments();
			GenerateColors(level.files.Count);
			GenerateSegmentObjects();
			GameController.Instance.DebugOutput($"Segments: {level.segments.Count()}\nFiles: {level.files.Count}");
		}

		void InitializeSegments() {
			GenerateColors(level.files.Count());
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
			transform.localPosition = new Vector3(-level.width / 2.0f, 13, 0.1f);
			transform.parent = gameObject.transform;

			foreach (SegmentData segment in level.segments) {
				GameObject segmentObject = Instantiate(segmentPrefab, transform);
				segmentObject.name = $"Seg_{segment}";
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
			int x = location % level.width;
			int y = location / level.width;
			var position = new Vector3(x, -y, 0);
			return position;
		}

		int? LocationForPosition(Vector2 position) {
			if (position.x < -0.5f || position.x > level.width + 0.5f) {
				return null;
			}
			if (position.y > 0.5f) {
				return null;
			}

			int result = (int)Mathf.Round(position.x - 0.5f) + level.width * (int)Mathf.Round(Mathf.Abs(position.y));
			return result;
		}

		#endregion


		void Update() {
			HandleInput();
		}

		void HandleInput() {
			if (!segmentsContainer) {
				return;
			}

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
			// determine potential target
			Plane plane = new Plane(Vector3.forward, 0);
			float enter = 0.0f;

			int? potentialTargetLocation = null;
			if (selectedSegment) {
				if (plane.Raycast(ray, out enter)) {
					//Get the point that is clicked
					Vector3 hitPoint = ray.GetPoint(enter);

					Vector3 localPosition = segmentsContainer.transform.InverseTransformPoint(hitPoint);
					potentialTargetLocation = LocationForPosition(localPosition);

					if (potentialTargetLocation.HasValue) {
						int? suggestedLocation = null;
						if (!level.CanCopySegmentToLocation(selectedSegment.segmentData, potentialTargetLocation.Value, out suggestedLocation)) {
							potentialTargetLocation = suggestedLocation;
						}
					}
				}
			}

			if (!selectedSegment) {
				SetHoverSegment(segmentUnderMouse);
			} else {
				if (segmentUnderMouse) {
					temporaryMoveSegment.SetActive(false);
				} else {
					if (potentialTargetLocation.HasValue) {
						//Move your cube GameObject to the point where you clicked
						temporaryMoveSegment.transform.localPosition = PositionForLocation(potentialTargetLocation.Value);
						temporaryMoveSegment.SetActive(true);
					}
				}
			}

			if (Input.GetButtonDown("Fire1")) {
				if (hoverSegment) {
					SetSelectedSegment(hoverSegment);
				} else if (selectedSegment) {
					if (potentialTargetLocation.HasValue && !segmentUnderMouse) {
						PlaceTemporarySegment(potentialTargetLocation.Value);
					} else {
						SetSelectedSegment(null);
					}
				}
			}

			if (Input.GetButtonUp("Fire1")) {
				if (selectedSegment) {
					if (potentialTargetLocation.HasValue && !segmentUnderMouse) {
						PlaceTemporarySegment(potentialTargetLocation.Value);
					}
				}
			}

			if (Input.GetButtonDown("Fire2")) {
				if (selectedSegment) {
					SetSelectedSegment(null);
				}
			}

		}

		void PlaceTemporarySegment(int location) {
			level.CopySegmentToLocation(selectedSegment.segmentData, location);
			// TODO: make this time based and wait for the copy to have finished to do so
			selectedSegment.transform.localPosition = PositionForLocation(location);
			SetSelectedSegment(null);
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