import queue
import threading
import webbrowser
from pathlib import Path

import customtkinter as ctk
try:
    from PIL import Image
    _PIL_OK = True
except ImportError:
    _PIL_OK = False

try:
    import pystray
    _PYSTRAY_OK = True
except ImportError:
    _PYSTRAY_OK = False

from core.config import Config
from core.organizer import Organizer
from core.updater import check_for_updates

from ui.pages.dashboard import DashboardPage
from ui.pages.rules import RulesPage
from ui.pages.exclusions import ExclusionsPage
from ui.pages.logs import LogsPage
from ui.pages.settings import SettingsPage

_APP_DIR = Path(__file__).parent.parent

_NAV = [
    ("dashboard",  "   Panel"),
    ("rules",      "   Reglas"),
    ("exclusions", "   Exclusiones"),
    ("logs",       "   Registros"),
    ("settings",   "   Ajustes"),
]

_PAGE_MAP = {
    "dashboard":  DashboardPage,
    "rules":      RulesPage,
    "exclusions": ExclusionsPage,
    "logs":       LogsPage,
    "settings":   SettingsPage,
}


class MainWindow(ctk.CTk):
    def __init__(self, config: Config, version: str):
        super().__init__()
        self.config = config
        self.version = version
        self._queue: queue.Queue = queue.Queue()
        self.organizer = Organizer(config, self._queue)
        self._pages: dict = {}
        self._nav_btns: dict = {}
        self._current_page: str | None = None
        self._update_banner: ctk.CTkFrame | None = None
        self._tray = None
        self._first_hide = True

        self._setup_window()
        self._build_sidebar()
        self._build_content()
        self._show_page("dashboard")
        self._poll()
        self._setup_tray()

        if config.get("auto_start_monitoring"):
            self.after(600, self.organizer.start)

        # Precarga diferida: construye el resto de páginas de fondo
        # para que el cambio de página sea instantáneo
        self.after(800, self._prebuild_next)

        self.after(2000, self._check_updates)

    # ── window ────────────────────────────────────────────────────────────────

    def _setup_window(self):
        self.title(f"Organizador de Descargas  v{self.version}")
        self.geometry("1150x720")
        self.minsize(900, 600)
        self.grid_columnconfigure(1, weight=1)
        self.grid_rowconfigure(0, weight=1)
        self.protocol("WM_DELETE_WINDOW", self._on_close)

    def _on_close(self):
        if self._tray:
            self.withdraw()
            if self._first_hide:
                self._first_hide = False
                try:
                    self._tray.notify(
                        "El organizador sigue corriendo en la bandeja del sistema.",
                        "Organizador de Descargas",
                    )
                except Exception:
                    pass
        else:
            if self.organizer.is_running:
                self.organizer.stop()
            self.destroy()

    # ── system tray ───────────────────────────────────────────────────────────

    def _setup_tray(self):
        if not _PYSTRAY_OK or not _PIL_OK:
            return
        try:
            logo = _APP_DIR / "Logotipotransparente.png"
            img = Image.open(logo).convert("RGBA") if logo.exists() else Image.new("RGBA", (64, 64), (30, 130, 200, 255))
            menu = pystray.Menu(
                pystray.MenuItem("Abrir", self._tray_show, default=True),
                pystray.Menu.SEPARATOR,
                pystray.MenuItem(
                    lambda item: "Pausar" if self.organizer.is_running else "Reanudar",
                    self._tray_toggle,
                ),
                pystray.Menu.SEPARATOR,
                pystray.MenuItem("Salir", self._tray_quit),
            )
            self._tray = pystray.Icon("OrganizadorDescargas", img, "Organizador de Descargas", menu)
            threading.Thread(target=self._tray.run, daemon=True).start()
        except Exception:
            self._tray = None

    def _tray_show(self, icon=None, item=None):
        self.after(0, self._restore_window)

    def _restore_window(self):
        self.deiconify()
        self.lift()
        self.focus_force()

    def _tray_toggle(self, icon=None, item=None):
        if self.organizer.is_running:
            self.after(0, self.organizer.stop)
        else:
            self.after(0, self.organizer.start)

    def _tray_quit(self, icon=None, item=None):
        if self.organizer.is_running:
            self.organizer.stop()
        if self._tray:
            self._tray.stop()
        self.after(0, self.destroy)

    # ── sidebar ───────────────────────────────────────────────────────────────

    def _build_sidebar(self):
        sb = ctk.CTkFrame(self, width=210, corner_radius=0,
                          fg_color=("gray18", "gray12"))
        sb.grid(row=0, column=0, sticky="nsew")
        sb.grid_propagate(False)
        sb.grid_rowconfigure(99, weight=1)
        self._sb = sb

        # Logo + título centrados en la parte superior
        logo_path = _APP_DIR / "Logotipotransparente.png"
        if _PIL_OK and logo_path.exists():
            img = ctk.CTkImage(Image.open(logo_path), size=(72, 72))
            ctk.CTkLabel(sb, image=img, text="").grid(
                row=0, column=0, padx=0, pady=(28, 6), sticky="n"
            )
            title_row = 1
        else:
            title_row = 0

        # Title
        title_frame = ctk.CTkFrame(sb, fg_color="transparent")
        title_frame.grid(row=title_row, column=0, padx=18,
                         pady=(14 if title_row == 0 else 4, 0), sticky="ew")
        ctk.CTkLabel(
            title_frame, text="Organizador",
            font=ctk.CTkFont(size=16, weight="bold"),
            text_color=("gray96", "gray93"),
        ).pack(side="left")
        ctk.CTkLabel(
            title_frame, text=f" v{self.version}",
            font=ctk.CTkFont(size=11),
            text_color=("gray52", "gray48"),
        ).pack(side="left", pady=(4, 0))

        ctk.CTkLabel(
            sb, text="Descargas",
            font=ctk.CTkFont(size=11),
            text_color=("gray52", "gray48"),
        ).grid(row=title_row + 1, column=0, padx=22, pady=(0, 12), sticky="w")

        ctk.CTkFrame(sb, height=1, fg_color=("gray30", "gray22")).grid(
            row=title_row + 2, column=0, padx=14, pady=(0, 10), sticky="ew"
        )

        nav_start = title_row + 3
        for idx, (key, label) in enumerate(_NAV):
            btn = ctk.CTkButton(
                sb, text=label, anchor="w",
                height=40, corner_radius=8, border_spacing=10,
                fg_color="transparent",
                text_color=("gray78", "gray72"),
                hover_color=("gray28", "gray22"),
                font=ctk.CTkFont(size=13),
                command=lambda k=key: self._show_page(k),
            )
            btn.grid(row=nav_start + idx, column=0, padx=10, pady=2, sticky="ew")
            self._nav_btns[key] = btn

        # Status indicator
        ctk.CTkFrame(sb, height=1, fg_color=("gray30", "gray22")).grid(
            row=98, column=0, padx=14, pady=(0, 8), sticky="ew"
        )
        status_frame = ctk.CTkFrame(sb, fg_color="transparent")
        status_frame.grid(row=99, column=0, padx=16, pady=(0, 20), sticky="sew")

        self._dot = ctk.CTkLabel(
            status_frame, text="●", font=ctk.CTkFont(size=13),
            text_color=("gray42", "gray38"),
        )
        self._dot.pack(side="left")
        self._status_lbl = ctk.CTkLabel(
            status_frame, text=" Detenido",
            font=ctk.CTkFont(size=12),
            text_color=("gray52", "gray48"),
        )
        self._status_lbl.pack(side="left")

    # ── content ───────────────────────────────────────────────────────────────

    def _build_content(self):
        self._content = ctk.CTkFrame(self, corner_radius=0,
                                     fg_color=("gray93", "gray11"))
        self._content.grid(row=0, column=1, sticky="nsew")
        self._content.grid_columnconfigure(0, weight=1)
        self._content.grid_rowconfigure(0, weight=1)

    # ── page switching ────────────────────────────────────────────────────────

    def _show_page(self, key: str):
        if self._current_page == key:
            return
        if self._current_page:
            self._nav_btns[self._current_page].configure(
                fg_color="transparent",
                text_color=("gray78", "gray72"),
            )
        self._nav_btns[key].configure(
            fg_color=("gray30", "gray24"),
            text_color=("gray98", "white"),
        )
        self._current_page = key
        self._ensure_page(key)

        for p in self._pages.values():
            p.grid_remove()
        self._pages[key].grid()

        if hasattr(self._pages[key], "on_show"):
            self._pages[key].on_show()

    def _ensure_page(self, key: str):
        if key not in self._pages:
            page = _PAGE_MAP[key](self._content, self)
            page.grid(row=0, column=0, sticky="nsew")
            self._pages[key] = page

    def _prebuild_next(self):
        """Construye silenciosamente la siguiente página sin renderizar."""
        pending = [k for k in _PAGE_MAP if k not in self._pages]
        if not pending:
            return
        self._ensure_page(pending[0])
        self._pages[pending[0]].grid_remove()
        self.after(200, self._prebuild_next)

    # ── event bus ─────────────────────────────────────────────────────────────

    def _poll(self):
        try:
            while True:
                evt = self._queue.get_nowait()
                self._handle(evt)
        except Exception:
            pass
        self.after(150, self._poll)

    def _handle(self, evt: dict):
        kind = evt.get("type")
        data = evt.get("data")
        if kind == "status":
            self._update_status(data)
        elif kind == "log":
            self._to_page("logs", "append_log", data)
            self._to_page("dashboard", "append_activity", data)
        elif kind == "file_moved":
            self._to_page("dashboard", "on_file_moved", data)

    def _to_page(self, key, method, data):
        page = self._pages.get(key)
        if page and hasattr(page, method):
            getattr(page, method)(data)

    def _update_status(self, status: str):
        if status == "running":
            self._dot.configure(text_color="#2CC985")
            self._status_lbl.configure(text=" Activo")
        else:
            self._dot.configure(text_color=("gray42", "gray38"))
            self._status_lbl.configure(text=" Detenido")
        if "dashboard" in self._pages:
            self._pages["dashboard"].update_status(status)

    # ── updates ───────────────────────────────────────────────────────────────

    def _check_updates(self):
        url = self.config.get("update_check_url", "")
        if url:
            check_for_updates(
                self.version, url,
                lambda r: self.after(0, lambda: self._on_update(r)),
            )

    def _on_update(self, result):
        if result and result.get("version"):
            self._show_update_banner(result)

    def _show_update_banner(self, info: dict):
        if self._update_banner:
            return
        banner = ctk.CTkFrame(self, height=44, corner_radius=0, fg_color="#1a5c38")
        banner.grid(row=1, column=0, columnspan=2, sticky="ew")
        banner.grid_propagate(False)
        self._update_banner = banner

        ctk.CTkLabel(
            banner,
            text=f"  Nueva versión {info['version']} disponible",
            text_color="white", font=ctk.CTkFont(size=13),
        ).pack(side="left", padx=16)

        ctk.CTkButton(
            banner, text="Descargar", width=100, height=28,
            fg_color="#0d3d22", hover_color="#08291a",
            command=lambda: webbrowser.open(info["url"]),
        ).pack(side="right", padx=14, pady=7)

        ctk.CTkButton(
            banner, text="×", width=28, height=28,
            fg_color="transparent", hover_color="#0d3d22",
            font=ctk.CTkFont(size=16),
            command=self._dismiss_banner,
        ).pack(side="right", pady=7)

    def _dismiss_banner(self):
        if self._update_banner:
            self._update_banner.destroy()
            self._update_banner = None
