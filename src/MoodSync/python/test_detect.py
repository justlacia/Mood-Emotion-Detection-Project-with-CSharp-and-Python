import unittest
from detect import select_detection


class DetectionTests(unittest.TestCase):
    def test_valid(self):
        self.assertEqual(select_detection([("happy", 0.9)]), ("positive", 0.9))

    def test_no_face_not_neutral(self):
        for detections in ([], [("neutral", 0.2)], [("unknown", 0.9)]):
            with self.assertRaises(ValueError):
                select_detection(detections)

    def test_multiple_faces_not_positive_by_priority(self):
        with self.assertRaises(ValueError):
            select_detection([("happy", 0.9), ("sad", 0.85)])


if __name__ == "__main__":
    unittest.main()
