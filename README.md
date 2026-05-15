<div align="center">
  <img src="Logotipotransparente.png" width="100" alt="Logo"/>

  # Organizador de Descargas
  **Organiza tu carpeta de Descargas automáticamente**

  ![Version](https://img.shields.io/badge/versión-2.0.0-blue)
  ![Platform](https://img.shields.io/badge/plataforma-Windows-lightgrey)
  ![Python](https://img.shields.io/badge/Python-3.10%2B-yellow)
</div>

---

## ¿Qué es?

Organizador de Descargas es una aplicación de escritorio para Windows que vigila tu carpeta de Descargas en tiempo real y mueve cada archivo automáticamente a la subcarpeta correcta según su extensión.

Descarga un `.pdf`? Va a **Documentos**. Un `.mp4`? A **Videos**. Un `.zip`? A **Comprimidos**. Todo sin que tengas que hacer nada.

---

## Descarga

> **[⬇ Descargar OrganizadorDescargas.exe](../../releases/latest)**

Solo descarga el `.exe`, ejecútalo y listo. No requiere instalación ni Python.

---

## Características

- **Monitoreo en tiempo real** — detecta archivos nuevos al instante usando watchdog
- **Reglas totalmente personalizables** — crea tus propias carpetas y asigna las extensiones que quieras
- **Exclusiones** — ignora extensiones o nombres de archivo específicos (`.crdownload`, `desktop.ini`, etc.)
- **Panel de actividad** — historial de todos los archivos movidos con hora y categoría
- **Registro de eventos** — log completo con filtros por tipo
- **Tema claro y oscuro** — se adapta a tu preferencia
- **Notificaciones de actualización** — te avisa cuando hay una nueva versión disponible
- **Ligero** — un solo archivo `.exe`, sin instalación, sin dependencias

---

## Capturas de pantalla

> *(próximamente)*

---

## Uso

1. Ejecuta `OrganizadorDescargas.exe`
2. La app empieza a monitorear tu carpeta `Descargas` automáticamente
3. Desde el panel **Reglas** puedes crear carpetas personalizadas y asignar extensiones
4. Desde **Exclusiones** puedes indicar qué archivos nunca se deben mover
5. El panel **Registros** muestra toda la actividad en tiempo real

---

## Compilar desde el código fuente

Si prefieres ejecutarlo desde Python:

```bash
# Clonar el repositorio
git clone https://github.com/Gatoxsempay/Smart-Downloader-Organizer.git
cd Smart-Downloader-Organizer

# Instalar dependencias
pip install -r requirements.txt

# Ejecutar
python app.py
```

Para generar el `.exe`:

```bash
pip install pyinstaller
pyinstaller --onefile --windowed --name "OrganizadorDescargas" --add-data "Logotipotransparente.png;." app.py
```

---

## Tecnologías

| Librería | Uso |
|---|---|
| [customtkinter](https://github.com/TomSchimansky/CustomTkinter) | Interfaz gráfica |
| [watchdog](https://github.com/gorakhargosh/watchdog) | Monitoreo del sistema de archivos |
| [Pillow](https://python-pillow.org/) | Carga del logo |
| [requests](https://requests.readthedocs.io/) | Comprobación de actualizaciones |
| [packaging](https://packaging.pypa.io/) | Comparación de versiones |

---

## Configuración

La configuración se guarda en:
```
%APPDATA%\OrganizadorDescargas\config.json
```

Los registros se guardan en:
```
%APPDATA%\OrganizadorDescargas\organizador.log
```

---

<div align="center">
  Hecho con Python · Windows 10/11
</div>
