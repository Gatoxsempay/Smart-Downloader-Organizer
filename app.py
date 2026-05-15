# Organizador Inteligente de Descargas
# Copyright (c) 2026 Fernando Alonso Maldonado Rivero. Todos los derechos reservados.
# Licencia: CC BY-NC-ND 4.0 — https://creativecommons.org/licenses/by-nc-nd/4.0/deed.es

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))

APP_VERSION = "2.0.0"


def _check_deps():
    missing = []
    for pkg in ("customtkinter", "watchdog"):
        try:
            __import__(pkg)
        except ImportError:
            missing.append(pkg)
    if missing:
        import subprocess
        print(f"Instalando dependencias: {', '.join(missing)} ...")
        subprocess.check_call([sys.executable, "-m", "pip", "install", *missing, "--quiet"])


def main():
    _check_deps()

    import customtkinter as ctk
    from core.config import Config
    from ui.main_window import MainWindow

    config = Config()
    ctk.set_appearance_mode(config.get("theme", "dark"))
    ctk.set_default_color_theme("blue")

    window = MainWindow(config, APP_VERSION)
    window.mainloop()


if __name__ == "__main__":
    main()
