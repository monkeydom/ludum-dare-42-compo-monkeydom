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

		[Space(5)]
		public Transform BorderLeft;
		public Transform BorderRight;
		public Transform BorderTop;
		public Transform BorderBottomLong;
		public Transform BorderBottomShort;

		List<Material> segmentColor;

		// Use this for initialization
		public void Start() {
			level = new Level(34, 34 * 20 + 13, Random.Range(3, 5), Random.Range(16, 60));
			//			InitializeSegments();
			GenerateColors(level.files.Count);
			GenerateSegmentObjects();
			AdjustLevelBoundaries();
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
			segmentsContainer = new GameObject("SegmentsContainer");

			Transform transform = segmentsContainer.transform;
			transform.localPosition = new Vector3(-level.width / 2.0f, 13, -1f);
			transform.localScale = new Vector3(1f, 1f, 1.8f);
			transform.parent = gameObject.transform;

			foreach (SegmentData segment in level.segments) {
				GameObject segmentObject = Instantiate(segmentPrefab, transform);
				segmentObject.name = $"Seg_{segment}";
				var sb = segmentObject.GetComponent<SegmentBehavior>();
				sb.SetSegmentData(segment);
				segmentObject.transform.localPosition = PositionForLocation(segment.location);
				foreach (MeshRenderer renderer in segmentObject.GetComponentsInChildren<MeshRenderer>(true)) {
					if (renderer.gameObject.tag == "SegmentObject") {
						renderer.sharedMaterial = segmentColor[segment.fileNumber];
					}
				}
			}
		}

		void AdjustLevelBoundaries() {
			Transform parent = BorderTop.parent;
			parent.position = segmentsContainer.transform.position;
			parent.localScale = segmentsContainer.transform.localScale;

			Transform containerTransform = segmentsContainer.transform;

			Vector3 center = containerTransform.TransformPoint(new Vector3(level.width / 2.0f, level.rowCount / -2.0f, 0));

			Vector3 verticalScale = new Vector3(1f, level.rowCount + 3.10f, 1f);

			Vector3 position;
			position = BorderLeft.position;
			position.x = containerTransform.TransformPoint(0.55f * Vector3.left).x;
			position.y = center.y;
			BorderLeft.position = position;
			BorderLeft.localScale = verticalScale;

			position = BorderRight.position;
			position.x = containerTransform.TransformPoint((level.width + 0.55f) * Vector3.right).x;
			position.y = center.y;
			BorderRight.position = position;
			BorderRight.localScale = verticalScale;


			Vector3 horizontalScale = new Vector3(level.width + 0.1f, 1, 1);
			position = BorderTop.position;
			position.y = containerTransform.TransformPoint(1.05f * Vector3.up).y;
			position.x = center.x;
			BorderTop.position = position;
			BorderTop.localScale = horizontalScale;

			position = BorderBottomLong.position;
			position.x = center.x;
			position.y = containerTransform.TransformPoint((level.rowCount + 1.05f) * Vector3.down).y;
			BorderBottomLong.position = position;
			BorderBottomLong.localScale = horizontalScale;

			int lastRowWidth = level.lastRowWidth;
			int lastRowRemainingWidth = level.width - lastRowWidth;
			position = BorderBottomShort.position;
			position.x = containerTransform.TransformPoint((lastRowWidth + lastRowRemainingWidth / 2.0f + 0.05f) * Vector3.right).x;
			position.y = containerTransform.TransformPoint((level.rowCount + 0.05f) * Vector3.down).y;
			BorderBottomShort.position = position;
			BorderBottomShort.localScale = new Vector3(lastRowRemainingWidth, 1, 1);



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


		Vector3 lastClickDownLocation;
		float lastClickDownTime;
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
			Vector3 localMousePosition = Vector3.zero;
			if (plane.Raycast(ray, out enter)) {
				//Get the point that is clicked
				Vector3 hitPoint = ray.GetPoint(enter);

				localMousePosition = segmentsContainer.transform.InverseTransformPoint(hitPoint);
				potentialTargetLocation = LocationForPosition(localMousePosition);

				if (selectedSegment) {
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
					UpdateTemporarySegmentPlacementIndication(null);
				} else {
					if (potentialTargetLocation.HasValue) {
						//Move your cube GameObject to the point where you clicked
						UpdateTemporarySegmentPlacementIndication(potentialTargetLocation.Value);
					}
				}
			}

			if (Input.GetButtonDown("Fire1")) {
				if (hoverSegment) {
					SetSelectedSegment(hoverSegment);
					lastClickDownTime = Time.realtimeSinceStartup;
					lastClickDownLocation = localMousePosition;
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
					bool allowDrag = (Time.realtimeSinceStartup - lastClickDownTime) > 1.0f / 6.0f ||
									 (localMousePosition - lastClickDownLocation).magnitude > 2.0;

					if (potentialTargetLocation.HasValue && !segmentUnderMouse && allowDrag) {
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

		void UpdateTemporarySegmentPlacementIndication(int? location) {
			if (location.HasValue) {
				var behavior = temporaryMoveSegment.GetComponent<SegmentBehavior>();
				behavior.AdjustLengthForIntRange(new IntRange(location.Value, behavior.segmentData.segmentLength));
				temporaryMoveSegment.transform.localPosition = PositionForLocation(location.Value);
				temporaryMoveSegment.SetActive(true);
			} else {
				temporaryMoveSegment.SetActive(false);
			}
		}

		void PlaceTemporarySegment(int location) {
			level.CopySegmentToLocation(selectedSegment.segmentData, location);
			// TODO: make this time based and wait for the copy to have finished to do so
			selectedSegment.transform.localPosition = PositionForLocation(location);
			selectedSegment.UpdateAppearanceForCurrentValues();
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
				foreach (Collider col in temporaryMoveSegment.GetComponentsInChildren<Collider>(true)) {
					col.enabled = false;
				}
				temporaryMoveSegment.name = "temporaryMoveSegment";
				UpdateTemporarySegmentPlacementIndication(null);
			} else {
				if (temporaryMoveSegment) {
					Destroy(temporaryMoveSegment);
				}
			}
		}
	}

}