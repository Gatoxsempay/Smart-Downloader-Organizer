import time
import shutil
import logging
import threading
import queue
from pathlib import Path
from typing import Optional

from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

from core.config import Config, CONFIG_DIR


def _setup_file_logger():
    log_path = CONFIG_DIR / "organizador.log"
    CONFIG_DIR.mkdir(parents=True, exist_ok=True)
    logger = logging.getLogger("organizador")
    logger.setLevel(logging.DEBUG)
    if not logger.handlers:
        fh = logging.FileHandler(log_path, encoding="utf-8", mode="a")
        fh.setFormatter(logging.Formatter("%(asctime)s  %(levelname)-8s  %(message)s", "%Y-%m-%d %H:%M:%S"))
        logger.addHandler(fh)
    return logger


class Organizer:
    def __init__(self, config: Config, event_queue: queue.Queue):
        self.config = config
        self.queue = event_queue
        self._observer: Optional[Observer] = None
        self._running = False
        self._logger = _setup_file_logger()

    @property
    def is_running(self) -> bool:
        return self._running

    def start(self):
        if self._running:
            return
        folder = Path(self.config.get("monitored_folder"))
        if not folder.exists():
            self._emit("log", {"message": f"Carpeta no existe: {folder}", "level": "ERROR"})
            return
        if self.config.get("organize_on_start"):
            threading.Thread(target=self._organize_existing, args=(folder,), daemon=True).start()
        handler = _Handler(self)
        self._observer = Observer()
        self._observer.schedule(handler, str(folder), recursive=False)
        self._observer.start()
        self._running = True
        self._emit("status", "running")
        self._log(f"Organizador iniciado — monitoreando: {folder}")

    def stop(self):
        if not self._running:
            return
        if self._observer:
            self._observer.stop()
            self._observer.join()
            self._observer = None
        self._running = False
        self._emit("status", "stopped")
        self._log("Organizador detenido.")

    def organize_now(self):
        folder = Path(self.config.get("monitored_folder"))
        threading.Thread(target=self._organize_existing, args=(folder,), daemon=True).start()

    def _organize_existing(self, folder: Path):
        self._log("Organizando archivos existentes...")
        count = 0
        for f in folder.iterdir():
            if f.is_file():
                self._move(f)
                count += 1
        self._log(f"Organización completada — {count} archivo(s) procesado(s).")

    def _move(self, path: Path):
        if not path.exists() or not path.is_file():
            return
        ext = path.suffix.lower()
        excl_ext = self.config.get("excluded_extensions", [])
        excl_names = [n.lower() for n in self.config.get("excluded_names", [])]
        excl_patterns = self.config.get("excluded_patterns", [])

        if ext in excl_ext:
            return
        if path.name.lower() in excl_names:
            return
        if path.name.startswith("."):
            return
        if any(p.lower() in path.name.lower() for p in excl_patterns if p):
            return

        rules = self.config.get("extension_rules", {})
        folder_name = rules.get(ext, "Otros")
        base = Path(self.config.get("monitored_folder"))
        dest_dir = base / folder_name
        dest_dir.mkdir(exist_ok=True)
        dest = self._no_collision(dest_dir, path.name)

        try:
            shutil.move(str(path), str(dest))
            self.config.increment_stat(folder_name)
            msg = f"Movido: {path.name}  →  {folder_name}/{dest.name}"
            self._log(msg)
            self._emit("file_moved", {"name": path.name, "category": folder_name, "dest": dest.name})
        except PermissionError:
            self._log(f"Sin permisos: {path.name} (archivo en uso)", "WARNING")
        except Exception as exc:
            self._log(f"Error al mover {path.name}: {exc}", "ERROR")

    @staticmethod
    def _no_collision(folder: Path, name: str) -> Path:
        dest = folder / name
        if not dest.exists():
            return dest
        stem, suffix = Path(name).stem, Path(name).suffix
        i = 1
        while True:
            candidate = folder / f"{stem} ({i}){suffix}"
            if not candidate.exists():
                return candidate
            i += 1

    def _log(self, msg: str, level: str = "INFO"):
        getattr(self._logger, level.lower(), self._logger.info)(msg)
        self._emit("log", {"message": msg, "level": level})

    def _emit(self, event_type: str, data=None):
        self.queue.put({"type": event_type, "data": data})


class _Handler(FileSystemEventHandler):
    def __init__(self, organizer: Organizer):
        self.org = organizer

    def on_created(self, event):
        if not event.is_directory:
            time.sleep(1.5)
            self.org._move(Path(event.src_path))

    def on_moved(self, event):
        if not event.is_directory:
            time.sleep(1.5)
            self.org._move(Path(event.dest_path))
