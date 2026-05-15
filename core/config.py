import json
import threading
from pathlib import Path

CONFIG_DIR = Path.home() / "AppData" / "Roaming" / "OrganizadorDescargas"
CONFIG_FILE = CONFIG_DIR / "config.json"

_DEFAULT_RULES = {
    ".pdf": "Documentos", ".docx": "Documentos", ".doc": "Documentos",
    ".txt": "Documentos", ".pptx": "Documentos", ".ppt": "Documentos",
    ".xlsx": "Documentos", ".xls": "Documentos", ".csv": "Documentos",
    ".odt": "Documentos", ".rtf": "Documentos",
    ".jpg": "Imágenes", ".jpeg": "Imágenes", ".png": "Imágenes",
    ".gif": "Imágenes", ".webp": "Imágenes", ".svg": "Imágenes",
    ".bmp": "Imágenes", ".ico": "Imágenes", ".tiff": "Imágenes",
    ".mp4": "Videos", ".mkv": "Videos", ".mov": "Videos",
    ".avi": "Videos", ".wmv": "Videos", ".flv": "Videos", ".webm": "Videos",
    ".mp3": "Audio", ".wav": "Audio", ".flac": "Audio",
    ".aac": "Audio", ".ogg": "Audio", ".wma": "Audio",
    ".exe": "Instaladores", ".msi": "Instaladores", ".msix": "Instaladores",
    ".appx": "Instaladores", ".dmg": "Instaladores", ".deb": "Instaladores",
    ".zip": "Comprimidos", ".rar": "Comprimidos", ".7z": "Comprimidos",
    ".tar": "Comprimidos", ".gz": "Comprimidos", ".xz": "Comprimidos",
}

_DEFAULTS = {
    "monitored_folder": str(Path.home() / "Downloads"),
    "auto_start_monitoring": True,
    "organize_on_start": True,
    "theme": "dark",
    "update_check_url": "",
    "extension_rules": _DEFAULT_RULES,
    "excluded_extensions": [".crdownload", ".tmp", ".part", ".download", ".opdownload"],
    "excluded_names": ["desktop.ini", "thumbs.db", ".ds_store", "organizador.log"],
    "excluded_patterns": [],
    "stats": {"total_moved": 0, "by_category": {}},
}


class Config:
    def __init__(self):
        self._lock = threading.Lock()
        self._data = {}
        self._version = 0
        self._load()

    @property
    def version(self) -> int:
        return self._version

    def _load(self):
        CONFIG_DIR.mkdir(parents=True, exist_ok=True)
        if CONFIG_FILE.exists():
            try:
                with open(CONFIG_FILE, "r", encoding="utf-8") as f:
                    stored = json.load(f)
                self._data = {**_DEFAULTS, **stored}
                self._data["extension_rules"] = {
                    **_DEFAULT_RULES,
                    **stored.get("extension_rules", {}),
                }
                self._data["stats"] = {
                    **_DEFAULTS["stats"],
                    **stored.get("stats", {}),
                }
                return
            except Exception:
                pass
        self._data = {k: (dict(v) if isinstance(v, dict) else v) for k, v in _DEFAULTS.items()}
        self._save_unlocked()

    def get(self, key, default=None):
        with self._lock:
            return self._data.get(key, default)

    def set(self, key, value):
        with self._lock:
            self._data[key] = value
            self._version += 1
        self.save()

    def save(self):
        CONFIG_DIR.mkdir(parents=True, exist_ok=True)
        with self._lock:
            self._save_unlocked()

    def _save_unlocked(self):
        with open(CONFIG_FILE, "w", encoding="utf-8") as f:
            json.dump(self._data, f, indent=2, ensure_ascii=False)

    def increment_stat(self, category: str):
        with self._lock:
            self._data["stats"]["total_moved"] = self._data["stats"].get("total_moved", 0) + 1
            cats = self._data["stats"].setdefault("by_category", {})
            cats[category] = cats.get(category, 0) + 1
            self._version += 1
        self.save()
