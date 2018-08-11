using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MonkeydomSpecific {

	public class FileData {
		public int fileID;
		public int fileLength;
		public List<SegmentData> segments;

		public FileData(Level level, int fileID, int fileLength) {
			this.fileID = fileID;
			this.fileLength = fileLength;

			segments = new List<SegmentData>();

			int segmentIndex = 0;
			int remainingLength = fileLength;
			while (remainingLength > 0) {
				int segmentLength = Random.Range(1, System.Math.Min(remainingLength, 16));
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

	public class Level : ScriptableObject {
		public int width;
		public int storageSpace;
		public List<SegmentData> segments;
		public List<FileData> files;
		List<IntRange> orderedFreeRanges;

		public Level(int width, int storageSpace, int fileCount, int fileLength) {
			this.width = width;
			this.storageSpace = storageSpace;
			GenerateFiles(fileCount, fileLength);

			int maxTries = 50;
			while (!DistributeSegments() && maxTries > 0) {
				maxTries--;
			}
		}

		void GenerateFiles(int fileCount, int fileLength) {
			segments = new List<SegmentData>();

			files = new List<FileData>();

			for (int index = 0; index < fileCount; index++) {
				FileData file = new FileData(this, index, Random.Range(10, fileLength));
				files.Add(file);
				segments.AddRange(file.segments);
			}
		}

		bool DistributeSegments() {
			orderedFreeRanges = new List<IntRange> { new IntRange(0, storageSpace) };
			int remainingTries = 100;
			foreach (SegmentData segment in segments) {
				bool placed = false;
				while (!placed) {
					int index = Random.Range(0, orderedFreeRanges.Count - 1);
					IntRange targetRange = orderedFreeRanges[index];
					int requiredLength = segment.segmentLength + 1;
					if (segment.partType == SegmentDataPartType.End) {
						requiredLength += 1;
					}
					if (targetRange.length < requiredLength) {
						remainingTries--;
					} else {
						// place
						IntRange placingRange = new IntRange(Random.Range(targetRange.location, targetRange.PositionAfter - requiredLength), requiredLength);
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
					if (remainingTries <= 0) return false;
				}
			}

			SortSegments();
			return true;
		}

		void SortSegments() {
			segments.Sort((a, b) => a.location.CompareTo(b.location));
		}

		public int CurrentEnd {
			get {
				// TODO: return end - timing based things that have been eaten
				return storageSpace;
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