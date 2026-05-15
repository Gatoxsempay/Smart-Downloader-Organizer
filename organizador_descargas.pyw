# Punto de entrada sin consola (doble clic para abrir la interfaz gráfica)
import runpy, pathlib, sys

sys.path.insert(0, str(pathlib.Path(__file__).parent))
runpy.run_path(str(pathlib.Path(__file__).parent / "app.py"), run_name="__main__")
