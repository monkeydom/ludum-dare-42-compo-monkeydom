﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace MonkeydomSpecific {

	public class FileData {
		public int fileID;
		public int fileLength;
		public List<SegmentData> segments;
		public Level level;

		public FileData(Level level, int fileID, int fileLength) {
			this.fileID = fileID;
			this.fileLength = fileLength;
			this.level = level;
			segments = new List<SegmentData>();

			int segmentIndex = 0;
			int remainingLength = fileLength;
			while (remainingLength > 0) {
				int segmentLength = Random.Range(1, System.Math.Min(remainingLength, level.width - 8));
				if (segmentLength == remainingLength && segmentIndex == 0) {
					segmentLength = Random.Range(1, segmentLength - 1);
				}
				SegmentData segment = new SegmentData();
				segment.fileNumber = fileID;
				segment.segmentNumber = segmentIndex + 1;
				segment.segmentLength = segmentLength;
				segment.level = level;
				if (segmentIndex == 0) {
					segment.partType = SegmentDataPartType.Start;
				} else if (remainingLength == segmentLength) {
					segment.partType = SegmentDataPartType.End;
				} else {
					segment.partType = SegmentDataPartType.Middle;
				}
				segments.Add(segment);

				segmentIndex += 1;
				remainingLength -= segmentLength;
			}

		}

		public string fileName {
			get {
				return $"File{fileID}";
			}
		}

		public List<List<SegmentData>> SegmentRuns() {
			List<List<SegmentData>> result = new List<List<SegmentData>>();
			List<SegmentData> currentData = new List<SegmentData>();
			foreach (SegmentData segment in segments) {
				if (currentData.Count > 0) {
					if (currentData.Last().PositionAfter != segment.location ||
						currentData.Last().segmentNumber + 1 != segment.segmentNumber) {
						if (currentData.Count > 1) {
							result.Add(currentData.ToArray().ToList());
						}
						currentData.Clear();
					}
				}
				currentData.Add(segment);
			}
			if (currentData.Count > 1) {
				result.Add(currentData);
			}
			return result;
		}

		public int score {
			get {
				int result = 0;
				var segmentRuns = SegmentRuns();
				foreach (var segmentRun in segmentRuns) {
					result += segmentRun.Count * 100;
					if (segmentRun.Count > 3) {
						result += 50 * segmentRun.Count - 2;
					}
					if (segmentRun.Count == segments.Count) {
						result += 200 * segmentRun.Count;
					}
				}

				return result;
			}
		}

		public bool IsFinished {
			get {
				var segmentRuns = SegmentRuns();
				return segmentRuns.Count == 1 && segmentRuns.First().Count == segments.Count;
			}
		}

		public string statusString {
			get {
				string result = SegmentRuns().Aggregate("", (memo, run) =>
														memo + $"[{run.First().segmentNumber}-{run.Last().segmentNumber}]");

				return result;
			}
		}
	}


	public class IntRange {
		public int location;
		public int length;

		public IntRange(int location, int length) {
			this.location = location;
			this.length = length;
		}

		public int PositionAfter {
			get {
				return location + length;
			}
		}

		public List<IntRange> CutOutRange(IntRange rangeToCut) {
			var result = new List<IntRange>();
			if (rangeToCut.location > location) {
				result.Add(new IntRange(location, rangeToCut.location - location));
			}
			if (rangeToCut.PositionAfter < PositionAfter) {
				result.Add(new IntRange(rangeToCut.PositionAfter, PositionAfter - rangeToCut.PositionAfter));
			}
			return result;
		}
	}

	public enum LevelState {
		Running,
		Finished,
		GameOver
	}

	public class Level : ScriptableObject {
		public int width;
		public int storageSpace;
		public int eventualStorageSpace;
		public List<SegmentData> segments;
		public List<FileData> files;
		List<IntRange> orderedFreeRanges;
		public float remainingTime;
		public float dyingStartTime;

		public int rowCount {
			get {
				return (int)Mathf.Ceil(storageSpace / width);
			}
		}
		public int lastRowWidth {
			get {
				return storageSpace % width;
			}
		}

		public Level(int width, int storageSpace, int fileCount, int fileLength, float percentageDying, float timePerDyingSpace) {
			this.width = width;
			this.storageSpace = storageSpace;
			GenerateFiles(fileCount, fileLength);

			int maxTries = 50;
			while (!DistributeSegments() && maxTries > 0) {
				maxTries--;
			}

			int totalFreeSpaceRemaining = storageSpace - segments.Aggregate(0, (memo, next) => memo + next.segmentLength);
			int spaceThatWillDie = (int)Mathf.Ceil(percentageDying * totalFreeSpaceRemaining);

			Debug.Log($"Free space: {totalFreeSpaceRemaining} / {storageSpace} - {spaceThatWillDie}");

			eventualStorageSpace = storageSpace - spaceThatWillDie;
			this.remainingTime = timePerDyingSpace * spaceThatWillDie;

			dyingStartTime = remainingTime; //- 10.0f; // disable for now
		}

		public LevelState levelState {
			get {
				if (segments.Last().PositionAfter > CurrentEnd) {
					return LevelState.GameOver;
				}
				if (remainingTime <= 0f) {
					return LevelState.Finished;
				}
				if (files.All((file) => file.IsFinished) &&
					segments.Last().PositionAfter - 1 < eventualStorageSpace) {
					return LevelState.Finished;
				}

				return LevelState.Running;
			}
		}

		public int score {
			get {
				int fileScore = files.Aggregate(0, (memo, file) => memo + file.score);
				files.Sort((a, b) => a.segments.First().location.CompareTo(b.segments.First().location));
				int multiplicator = 1;
				for (int i = 0; i < files.Count - 1; i++) {
					if (files[0].segments.Last().PositionAfter == files[1].segments.First().location) {
						multiplicator += 1;
					}
				}
				int remainingScore = (CurrentEnd - segments.Last().PositionAfter) * 15;
				int additionalNonCorruptedScore = (CurrentEnd - eventualStorageSpace) * 50;
				int multipliedFileScore = fileScore * multiplicator;
				int score = multipliedFileScore + remainingScore + additionalNonCorruptedScore;
				Debug.Log($"Level score: files:{fileScore} filesMultiplied:{multipliedFileScore} remaining:{remainingScore} nonCorrupted:{additionalNonCorruptedScore} - total: {score}");
				return score;
			}
		}

		public void AdvanceTime(float deltaTime) {
			remainingTime -= deltaTime;
			if (remainingTime < 0) {
				remainingTime = 0f;
			}
		}

		void GenerateFiles(int fileCount, int fileLength) {
			segments = new List<SegmentData>();

			files = new List<FileData>();
			int remainingSpace = storageSpace - 10;
			for (int index = 0; index < fileCount; index++) {
				int testLength = Random.Range(Mathf.Max(fileLength - width, 7), fileLength);
				if (testLength > remainingSpace / 2.0f) {
					testLength = Mathf.FloorToInt(remainingSpace / 2.0f);
				}
				FileData file = new FileData(this, index, testLength);
				files.Add(file);
				segments.AddRange(file.segments);
				remainingSpace -= file.fileLength;
			}
		}

		bool DistributeSegments() {
			orderedFreeRanges = new List<IntRange> { new IntRange(0, storageSpace - 10) };

			foreach (SegmentData segment in segments) {
				bool placed = false;
				while (!placed) {
					int requiredLength = segment.segmentLength;
					if (segment.partType == SegmentDataPartType.End) {
						requiredLength += 1;
					}

					var validRanges = orderedFreeRanges.Where((range) => range.length >= requiredLength).ToList();

					IntRange targetRange = validRanges[Mathf.FloorToInt(Random.Range(0, validRanges.Count - 0.1f))];
					int index = orderedFreeRanges.IndexOf(targetRange);


					int randomStart = targetRange.location;
					if (targetRange.length > requiredLength) {
						randomStart += 1;
					}
					// place
					IntRange placingRange = new IntRange(Random.Range(randomStart, targetRange.PositionAfter - requiredLength), requiredLength);
					segment.location = placingRange.location;
					placed = true;

					// reduce placement options
					var resultingRanges = targetRange.CutOutRange(placingRange);
					if (resultingRanges.Count == 0) {
						orderedFreeRanges.RemoveAt(index);
					} else {
						orderedFreeRanges[index] = resultingRanges[0];
						if (resultingRanges.Count == 2) {
							orderedFreeRanges.Insert(index + 1, resultingRanges[1]);
						}
					}
				}
			}

			SortSegments();
			return true;
		}

		void SortSegments() {
			segments.Sort((a, b) => a.location.CompareTo(b.location));
			foreach (var fileData in files) {
				fileData.segments.Sort((a, b) => a.location.CompareTo(b.location));
			}
		}

		public float dyingMemoryPosition {
			get {
				if (remainingTime > dyingStartTime) {
					return storageSpace;
				} else {
					return Mathf.Lerp(storageSpace, eventualStorageSpace, 1.0f - remainingTime / dyingStartTime);
				}
			}
		}

		public int CurrentEnd {
			get {
				return Mathf.FloorToInt(dyingMemoryPosition);
			}
		}

		public List<IntRange> ExtentsForIntRange(IntRange range) {
			int positionAfter = range.PositionAfter;
			var result = new List<IntRange> { range, null };

			int startLine = range.location / width;
			int endLine = (range.PositionAfter - 1) / width;

			if (startLine != endLine) {
				int lengthInSameLine = width - (range.location % width);
				result[0].length = lengthInSameLine;
				result[1] = new IntRange(result[0].PositionAfter, positionAfter - result[0].PositionAfter);
			}
			return result;
		}

		public List<SegmentData> SegmentsSurroundingLocation(int location) {
			List<SegmentData> result = new List<SegmentData> { null, null };
			int segmentCount = segments.Count;
			for (int index = 0; index < segmentCount; index++) {
				SegmentData segment = segments[index];
				if (segment.location < location) {
					result[0] = segment;
				}
				if (segment.location >= location) {
					result[1] = segment;
					break;
				}
			}
			return result;
		}

		public bool CanCopySegmentToLocation(SegmentData segment, int location, out int? suggestedLocation) {
			suggestedLocation = null;

			if (location < 0) {
				return false;
			}
			if (location + segment.segmentLength > CurrentEnd) {
				var earliestEndStart = segments[segments.Count - 1].PositionAfter;
				if (CurrentEnd - earliestEndStart > segment.segmentLength) {
					suggestedLocation = CurrentEnd - segment.segmentLength;
				}
				return false;
			}

			var enclosingSegments = SegmentsSurroundingLocation(location);
			// Debug.Log($"Enclosing Segments: {enclosingSegments[0]} - {location} - {enclosingSegments[1]}");
			if (enclosingSegments[0] && enclosingSegments[0].PositionAfter > location) {
				return false;
			}
			if (enclosingSegments[1] && enclosingSegments[1].location < location + segment.segmentLength) {
				if (!enclosingSegments[0] || (enclosingSegments[1].location - enclosingSegments[0].PositionAfter) >= segment.segmentLength) {
					int potentialLocation = enclosingSegments[1].location - segment.segmentLength;
					if (potentialLocation >= 0) {
						suggestedLocation = potentialLocation;
					}
				}
				return false;
			}

			return true;
		}

		// TODO: make it take time and give a progress callback
		public void CopySegmentToLocation(SegmentData segment, int location) {
			segment.location = location;
			SortSegments();
		}

	}
}