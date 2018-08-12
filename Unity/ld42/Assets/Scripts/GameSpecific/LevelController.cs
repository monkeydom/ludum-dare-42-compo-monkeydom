using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace MonkeydomSpecific {

	public enum LevelControllerState {
		Paused,
		Running,
		GameOver,
	}

	public class LevelController : MonoBehaviour {

		SegmentBehavior hoverSegment;
		SegmentBehavior selectedSegment;

		List<SegmentBehavior> hoverSegments;
		List<SegmentBehavior> selectedSegments;

		GameObject temporaryMoveSegment;

		Level level;
		int stage;
		int score = 0;

		LevelControllerState state;

		[Header("Outlets")]
		public Camera mainCamera;
		public GameObject segmentPrefab;
		public GameObject deadStoragePrefab;
		public GameObject fileStatusPrefab;
		public Material segmentBaseColor;
		public Material[] segmentColorPalette;
		public GameObject segmentsContainer;
		public GameObject fileStatusContainer;

		[Space(5)]
		public Transform BorderLeft;
		public Transform BorderRight;
		public Transform BorderTop;
		public Transform BorderBottomLong;
		public Transform BorderBottomShort;

		List<Material> segmentColor;
		List<FileStatusBehavior> fileStatusBehaviors;

		List<DeadStorageBehavior> deadStorageBehaviors;

		// Use this for initialization
		public void Start() {
			StartGame();
		}

		void GenerateLevel(int stage) {
			int maxFileCount = Mathf.Min(stage + 1, 7);
			int fileCount = Random.Range(Mathf.Max(2, maxFileCount - 2), maxFileCount);
			int width = Mathf.Min(12 + stage / 2, 30);
			int maxFileLength = Mathf.Min(width * 2 - 1, Mathf.Max(stage * 3, 12));
			int storageSpace = width * 6 + stage * 17;
			storageSpace = Mathf.FloorToInt(Random.Range(storageSpace, storageSpace * 1.4f));
			storageSpace = Mathf.Min(27 * width - 1, storageSpace);
			float precentageOfDyingSpace = Random.Range(0.7f, Mathf.Min(0.7f + stage * 0.06f, 0.95f));
			state = LevelControllerState.Running;
			float timePerDie = Mathf.Max(0.4f, (3.5f - Mathf.Log(stage) * 0.9f));

			maxFileLength = Mathf.Min(maxFileLength, (storageSpace - 2 * width) / fileCount);

			Debug.Log($"stage: {stage} width: {width}, storageSpace: {storageSpace}, fileCount: {fileCount}, maxFileLength: {maxFileLength}, timePerTile: {timePerDie}");
			level = new Level(width, storageSpace, fileCount, maxFileLength, precentageOfDyingSpace, timePerDie);

			GenerateColors(level.files.Count);
			GenerateSegmentObjects();
			GenerateDeadStorageIndicators();
			AdjustLevelBoundaries();
			GenerateFileStatusObjects();
			GameController.Instance.DebugOutput($"Segments: {level.segments.Count()}\nFiles: {level.files.Count}");
			UpdateScoreAndStage();
		}

		public void StartGame() {
			stage = 1;
			GenerateLevel(stage);
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
			transform.localPosition = new Vector3(-level.width / 2.0f, 13, 0f);
			transform.localScale = new Vector3(1f, 1f, 3.0f);
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

		void GenerateFileStatusObjects() {
			if (fileStatusContainer) {
				Destroy(fileStatusContainer);
			}
			fileStatusContainer = new GameObject("FileStatusContainer");

			Transform transform = fileStatusContainer.transform;
			transform.localPosition = new Vector3(-20f, 13f, -1f);
			transform.parent = gameObject.transform;

			fileStatusBehaviors = new List<FileStatusBehavior>();

			Vector3 position = Vector3.zero;
			Vector3 horizontalMovement = Vector3.left * 1.0f;
			Transform parent = fileStatusContainer.transform;

			foreach (FileData file in level.files) {
				GameObject go = Instantiate(fileStatusPrefab, parent);
				var behavior = go.GetComponent<FileStatusBehavior>();
				go.transform.localPosition = position;
				fileStatusBehaviors.Add(behavior);
				go.name = $"FileStatus-{file.fileID}";
				foreach (MeshRenderer renderer in go.GetComponentsInChildren<MeshRenderer>(true)) {
					if (renderer.gameObject.tag == "FileObject") {
						renderer.sharedMaterial = segmentColor[file.fileID];
					}
				}
				position += Vector3.down * 3.5f + horizontalMovement;
				horizontalMovement *= -1f;
			}
			UpdateFileStatusObjects();
		}

		void UpdateFileStatusObjects() {
			for (int i = 0; i < fileStatusBehaviors.Count; i++) {
				FileData file = level.files[i];
				fileStatusBehaviors[i].SetTexts(file.fileName, file.statusString, $"{file.score}");
			}
		}

		void GenerateDeadStorageIndicators() {
			Transform parent = segmentsContainer.transform;
			deadStorageBehaviors = new List<DeadStorageBehavior>();
			for (int location = level.eventualStorageSpace; location < level.storageSpace; location++) {
				GameObject go = Instantiate(deadStoragePrefab, parent);
				var behavior = go.GetComponent<DeadStorageBehavior>();
				go.transform.localPosition = PositionForLocation(location);
				behavior.SetProgress(0.0f);
				deadStorageBehaviors.Add(behavior);
				go.name = $"DeadStorage-{location}";
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

		void HandleGameOver() {
			state = LevelControllerState.GameOver;
			UpdateScoreAndStage();
		}

		void StartNextStage() {
			stage++;
			GenerateLevel(stage);
		}

		void ScoreLevel() {

		}

		void Update() {
			if (state == LevelControllerState.Running) {
				level.AdvanceTime(Time.deltaTime);
				float dyingMemoryPosition = level.dyingMemoryPosition;
				if (dyingMemoryPosition < level.storageSpace) {
					int deadStorageIndex = (int)Mathf.Floor(dyingMemoryPosition);
					int localIndex = deadStorageIndex - level.eventualStorageSpace;
					deadStorageBehaviors[localIndex].SetProgress(1.0f - (dyingMemoryPosition - deadStorageIndex));
				}
				UpdateStatDisplay();
				LevelState levelState = level.levelState;
				if (levelState == LevelState.Finished) {
					StartNextStage();
				} else if (levelState == LevelState.GameOver) {
					HandleGameOver();
				} else {
					HandleGamePlayInput();
				}
			}

			if (Input.GetButtonDown("Jump")) {
				if (state == LevelControllerState.Running) {
					StartNextStage();
				} else {
					StartGame();
				}
			}
		}

		void UpdateStatDisplay() {
			float remainingTime = level.remainingTime;
			float timeBeforeStaringDying = remainingTime - level.dyingStartTime;
			string timeString = $"{remainingTime.ToString("0.0")}";
			if (timeBeforeStaringDying > 0) {
				timeString = $"Disk decays in: {timeBeforeStaringDying.ToString("0.0")}";
			}
			GameController.Instance.SetLevelStatusText(timeString);
		}

		void UpdateScoreAndStage() {
			string text = $"Stage {stage} - {score.ToString("000000")}";

			if (state == LevelControllerState.GameOver) {
				text = $"Game Over - {text}";
			}
			GameController.Instance.SetTopRightText(text);
		}

		Vector3 lastClickDownLocation;
		float lastClickDownTime;
		void HandleGamePlayInput() {
			if (!segmentsContainer) {
				return;
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

			// clear segment unter mouse if temporary segment is in the way
			if (temporaryMoveSegment && temporaryMoveSegment.activeSelf) {
				foreach (Collider col in temporaryMoveSegment.GetComponentsInChildren<Collider>(true)) {
					col.enabled = true;
				}
				if (Physics.Raycast(ray, out hit, Mathf.Infinity, GameController.LayerMaskSegments)) {
					Transform objectHit = hit.transform;
					var hitGameObject = objectHit.parent.GetComponentInParent<SegmentBehavior>().gameObject;
					if (hitGameObject == temporaryMoveSegment) {
						segmentUnderMouse = null;
					}
				}
				foreach (Collider col in temporaryMoveSegment.GetComponentsInChildren<Collider>(true)) {
					col.enabled = false;
				}
			}

			if (!selectedSegment) {
				SetHoverSegment(segmentUnderMouse);
			} else {
				if (temporaryMoveSegment && temporaryMoveSegment.activeSelf && (temporaryMoveSegment.transform.localPosition - localMousePosition).magnitude > 3.0) {
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
					if (temporaryMoveSegment.activeSelf) {
						PlaceTemporarySegment();
					} else {
						SetSelectedSegment(null);
					}
				}
			}

			if (Input.GetButtonUp("Fire1")) {
				if (selectedSegment) {
					bool allowDrag = (Time.realtimeSinceStartup - lastClickDownTime) > 1.0f / 5.0f ||
									 (localMousePosition - lastClickDownLocation).magnitude > 0.75;

					if (temporaryMoveSegment.activeSelf && allowDrag) {
						PlaceTemporarySegment();
					} else if (Time.realtimeSinceStartup - lastClickDownTime > 1.0f / 2.0f) {
						SetSelectedSegment(null);
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
				selectedSegment.segmentData.temporaryTargetCopyPlacementLocation = location.Value;
				var behavior = temporaryMoveSegment.GetComponent<SegmentBehavior>();
				behavior.AdjustLengthForIntRange(new IntRange(location.Value, behavior.segmentData.segmentLength));
				temporaryMoveSegment.transform.localPosition = PositionForLocation(location.Value);
				temporaryMoveSegment.SetActive(true);
			} else {
				selectedSegment.segmentData.temporaryTargetCopyPlacementLocation = null;
				temporaryMoveSegment.SetActive(false);
			}
		}

		void PlaceTemporarySegment() {
			int location = selectedSegment.segmentData.temporaryTargetCopyPlacementLocation.Value;
			level.CopySegmentToLocation(selectedSegment.segmentData, location);
			// TODO: make this time based and wait for the copy to have finished to do so
			selectedSegment.transform.localPosition = PositionForLocation(location);
			selectedSegment.UpdateAppearanceForCurrentValues();
			SetSelectedSegment(null);
			UpdateFileStatusObjects();
		}

		void UpdateFileStatusHighlight() {
			int? highlightedFileID = null;
			if (selectedSegment) {
				highlightedFileID = selectedSegment.segmentData.fileNumber;
			} else if (hoverSegment) {
				highlightedFileID = hoverSegment.segmentData.fileNumber;
			}

			foreach (FileStatusBehavior behavior in fileStatusBehaviors) {
				behavior.highlighted = false;
			}

			if (highlightedFileID.HasValue) {
				fileStatusBehaviors[highlightedFileID.Value].highlighted = true;
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

				UpdateFileStatusHighlight();
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
				segment.ShowClick();
			} else {
				if (temporaryMoveSegment) {
					Destroy(temporaryMoveSegment);
				}
			}
			UpdateFileStatusHighlight();
		}
	}

}