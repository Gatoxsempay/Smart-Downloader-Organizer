# Organizador Inteligente de Descargas
# Copyright (c) 2026 Fernando Alonso Maldonado Rivero. Todos los derechos reservados.
# Licencia: CC BY-NC-ND 4.0 — https://creativecommons.org/licenses/by-nc-nd/4.0/deed.es

import sys
import time
import shutil
import logging
from pathlib import Path
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

# --- Configuración ---
CARPETA_DESCARGAS = Path.home() / "Downloads"

MAPA_EXTENSIONES = {
    # Documentos
    ".pdf":  "Documentos",
    ".docx": "Documentos",
    ".doc":  "Documentos",
    ".txt":  "Documentos",
    ".pptx": "Documentos",
    ".ppt":  "Documentos",
    ".xlsx": "Documentos",
    ".xls":  "Documentos",
    ".csv":  "Documentos",
    # Imágenes
    ".jpg":  "Imágenes",
    ".jpeg": "Imágenes",
    ".png":  "Imágenes",
    ".gif":  "Imágenes",
    ".webp": "Imágenes",
    ".svg":  "Imágenes",
    ".bmp":  "Imágenes",
    ".ico":  "Imágenes",
    # Videos
    ".mp4":  "Videos",
    ".mkv":  "Videos",
    ".mov":  "Videos",
    ".avi":  "Videos",
    ".wmv":  "Videos",
    # Audio
    ".mp3":  "Audio",
    ".wav":  "Audio",
    ".flac": "Audio",
    ".aac":  "Audio",
    # Instaladores
    ".exe":  "Instaladores",
    ".msi":  "Instaladores",
    ".msix": "Instaladores",
    ".appx": "Instaladores",
    ".dmg":  "Instaladores",
    ".deb":  "Instaladores",
    # Comprimidos
    ".zip":  "Comprimidos",
    ".rar":  "Comprimidos",
    ".7z":   "Comprimidos",
    ".tar":  "Comprimidos",
    ".gz":   "Comprimidos",
}

# Extensiones de archivos temporales/incompletos que NO se deben mover
EXTENSIONES_IGNORADAS = {".crdownload", ".tmp", ".part", ".download", ".opdownload"}

# Nombres de archivo que nunca se deben mover (archivos de sistema/ocultos)
NOMBRES_IGNORADOS = {"desktop.ini", "thumbs.db", ".ds_store"}

# --- Logger ---
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s  %(levelname)-8s  %(message)s",
    datefmt="%Y-%m-%d %H:%M:%S",
    handlers=[
        logging.FileHandler(
            Path.home() / "Downloads" / "organizador.log",
            encoding="utf-8",
            mode="a",
        ),
        logging.StreamHandler(sys.stdout),
    ],
)
log = logging.getLogger("organizador")


def destino_sin_colision(carpeta_destino: Path, nombre_archivo: str) -> Path:
    """Devuelve una ruta libre de colisiones añadiendo (1), (2)… si es necesario."""
    destino = carpeta_destino / nombre_archivo
    if not destino.exists():
        return destino

    stem = Path(nombre_archivo).stem
    suffix = Path(nombre_archivo).suffix
    contador = 1
    while True:
        candidato = carpeta_destino / f"{stem}({contador}){suffix}"
        if not candidato.exists():
            return candidato
        contador += 1


def mover_archivo(ruta: Path) -> None:
    """Clasifica y mueve un archivo a su carpeta correspondiente."""
    if not ruta.exists() or not ruta.is_file():
        return

    extension = ruta.suffix.lower()

    # Ignorar archivos temporales / descargas incompletas
    if extension in EXTENSIONES_IGNORADAS:
        log.debug("Ignorado (temporal): %s", ruta.name)
        return

    # Ignorar archivos de sistema y el propio log
    if ruta.name.lower() in NOMBRES_IGNORADOS or ruta.name == "organizador.log":
        return

    # Ignorar archivos ocultos (empiezan con punto)
    if ruta.name.startswith("."):
        return

    nombre_carpeta = MAPA_EXTENSIONES.get(extension, "Otros")
    carpeta_destino = CARPETA_DESCARGAS / nombre_carpeta
    carpeta_destino.mkdir(exist_ok=True)

    ruta_destino = destino_sin_colision(carpeta_destino, ruta.name)

    try:
        shutil.move(str(ruta), str(ruta_destino))
        log.info("Movido: %-40s  →  %s/%s", ruta.name, nombre_carpeta, ruta_destino.name)
    except PermissionError:
        log.warning("Sin permisos para mover: %s  (¿sigue en uso?)", ruta.name)
    except Exception as exc:
        log.error("Error al mover %s: %s", ruta.name, exc)


class ManejadorDescargas(FileSystemEventHandler):
    """Escucha eventos del sistema de archivos en la carpeta de Descargas."""

    def on_created(self, event):
        if event.is_directory:
            return
        ruta = Path(event.src_path)
        # Pequeña espera para que el archivo termine de escribirse antes de moverlo
        time.sleep(1)
        mover_archivo(ruta)

    def on_moved(self, event):
        # Chrome/Edge renombran el .crdownload al archivo final cuando termina la descarga
        if event.is_directory:
            return
        ruta = Path(event.dest_path)
        time.sleep(1)
        mover_archivo(ruta)


def organizar_existentes() -> None:
    """Mueve los archivos que ya estaban en Descargas al iniciar el script."""
    log.info("Organizando archivos existentes en %s …", CARPETA_DESCARGAS)
    for archivo in CARPETA_DESCARGAS.iterdir():
        if archivo.is_file():
            mover_archivo(archivo)
    log.info("Organización inicial completada.")


def main() -> None:
    log.info("=" * 60)
    log.info("Organizador de Descargas iniciado")
    log.info("Carpeta vigilada: %s", CARPETA_DESCARGAS)
    log.info("=" * 60)

    # Organizar lo que ya existe
    organizar_existentes()

    # Iniciar monitoreo en tiempo real
    manejador = ManejadorDescargas()
    observador = Observer()
    observador.schedule(manejador, str(CARPETA_DESCARGAS), recursive=False)
    observador.start()
    log.info("Monitoreo activo. Presiona Ctrl+C para detener.")

    try:
        while True:
            time.sleep(5)
    except KeyboardInterrupt:
        log.info("Deteniendo el organizador…")
        observador.stop()

    observador.join()
    log.info("Organizador detenido.")


if __name__ == "__main__":
    main()
