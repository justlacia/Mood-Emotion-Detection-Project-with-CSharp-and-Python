"""One request in, one JSON response out. Diagnostic output belongs on stderr."""
import contextlib
import json
import os
import sys
import tempfile
from pathlib import Path


def select_detection(detections):
    aliases = {"positive": "positive", "happy": "positive", "smile": "positive",
               "negative": "negative", "sad": "negative", "cry": "negative", "neutral": "neutral"}
    valid = [(aliases[label.lower()], float(score)) for label, score in detections
             if label.lower() in aliases and 0.5 <= float(score) <= 1]
    if not valid:
        raise ValueError("Yüz ifadesi yeterli güvenle tespit edilemedi. Daha net, tek yüz içeren bir fotoğraf deneyin.")
    if len(valid) > 1:
        raise ValueError("Birden fazla yüz bulundu. Tek kişinin bulunduğu bir fotoğraf seçin.")
    return valid[0]


def analyze(image_path):
    image = Path(image_path).resolve()
    if not image.is_file() or image.suffix.lower() not in {".jpg", ".jpeg", ".png"}:
        raise ValueError("Geçerli bir JPG veya PNG dosyası seçin.")
    if image.stat().st_size > 20 * 1024 * 1024:
        raise ValueError("Fotoğraf 20 MB sınırını aşıyor.")
    model_path = Path(__file__).parent / "models" / "model.pt"
    if not model_path.is_file():
        raise ValueError("Model bulunamadı: python/models/model.pt")
    cache = Path(tempfile.gettempdir()) / "MoodSync"
    cache.mkdir(parents=True, exist_ok=True)
    os.environ.setdefault("YOLO_CONFIG_DIR", str(cache))
    os.environ.setdefault("MPLCONFIGDIR", str(cache / "matplotlib"))
    with contextlib.redirect_stdout(sys.stderr):
        from ultralytics import YOLO
        model = YOLO(str(model_path))
        result = model.predict(source=str(image), conf=0.5, save=False, verbose=False)[0]
        boxes = result.boxes
        detections = [] if boxes is None else [(result.names[int(c)], float(p)) for c, p in zip(boxes.cls.tolist(), boxes.conf.tolist())]
    mood, confidence = select_detection(detections)
    return {"mood": mood, "confidence": confidence, "imagePath": str(image)}


def main():
    try:
        if len(sys.argv) != 2:
            raise ValueError("Bir fotoğraf dosyası yolu verilmeli.")
        print(json.dumps(analyze(sys.argv[1]), ensure_ascii=False))
        return 0
    except Exception as exc:
        print(json.dumps({"error": str(exc)}, ensure_ascii=False))
        return 1


if __name__ == "__main__":
    sys.exit(main())
