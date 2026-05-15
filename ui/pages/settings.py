import shutil
from pathlib import Path

import customtkinter as ctk
from tkinter import filedialog, messagebox

from core.autostart import get_autostart, set_autostart
from core.config import CONFIG_DIR


class SettingsPage(ctk.CTkFrame):
    def __init__(self, parent, window):
        super().__init__(parent, fg_color="transparent")
        self.window = window
        self.config = window.config
        self._build()

    def _build(self):
        self.grid_columnconfigure(0, weight=1)
        self.grid_rowconfigure(1, weight=1)

        ctk.CTkLabel(
            self, text="Ajustes",
            font=ctk.CTkFont(size=24, weight="bold"),
        ).grid(row=0, column=0, padx=30, pady=(30, 16), sticky="w")

        scroll = ctk.CTkScrollableFrame(self, fg_color="transparent")
        scroll.grid(row=1, column=0, padx=30, pady=(0, 30), sticky="nsew")
        scroll.grid_columnconfigure(0, weight=1)

        self._folder_section(scroll, row=0)
        self._behavior_section(scroll, row=1)
        self._appearance_section(scroll, row=2)
        self._danger_section(scroll, row=3)

    # ── helpers ───────────────────────────────────────────────────────────────

    def _card(self, parent, title: str, row: int) -> ctk.CTkFrame:
        frame = ctk.CTkFrame(parent, corner_radius=14)
        frame.grid(row=row, column=0, pady=(0, 14), sticky="ew")
        frame.grid_columnconfigure(0, weight=1)
        ctk.CTkLabel(
            frame, text=title,
            font=ctk.CTkFont(size=15, weight="bold"),
        ).grid(row=0, column=0, padx=22, pady=(20, 14), sticky="w")
        ctk.CTkFrame(frame, height=1, fg_color=("gray80", "gray25")).grid(
            row=1, column=0, padx=22, pady=(0, 14), sticky="ew"
        )
        return frame

    def _switch_row(self, parent, label: str, key: str, grid_row: int):
        r = ctk.CTkFrame(parent, fg_color="transparent")
        r.grid(row=grid_row, column=0, padx=22, pady=8, sticky="ew")
        r.grid_columnconfigure(0, weight=1)
        ctk.CTkLabel(r, text=label, font=ctk.CTkFont(size=13)).grid(row=0, column=0, sticky="w")
        sw = ctk.CTkSwitch(r, text="")
        sw.grid(row=0, column=1)
        if self.config.get(key):
            sw.select()
        sw.configure(command=lambda k=key, s=sw: self.config.set(k, bool(s.get())))

    # ── sections ──────────────────────────────────────────────────────────────

    def _folder_section(self, parent, row: int):
        card = self._card(parent, "Carpeta monitoreada", row)

        inner = ctk.CTkFrame(card, fg_color="transparent")
        inner.grid(row=2, column=0, padx=22, pady=(0, 20), sticky="ew")
        inner.grid_columnconfigure(0, weight=1)

        self._folder_entry = ctk.CTkEntry(inner, height=36, state="readonly")
        self._folder_entry.grid(row=0, column=0, sticky="ew", padx=(0, 10))
        self._folder_entry.configure(state="normal")
        self._folder_entry.insert(0, self.config.get("monitored_folder", ""))
        self._folder_entry.configure(state="readonly")

        def pick():
            path = filedialog.askdirectory(title="Seleccionar carpeta a monitorear")
            if not path:
                return
            self.config.set("monitored_folder", path)
            self._folder_entry.configure(state="normal")
            self._folder_entry.delete(0, "end")
            self._folder_entry.insert(0, path)
            self._folder_entry.configure(state="readonly")
            if self.window.organizer.is_running:
                self.window.organizer.stop()
                self.window.organizer.start()

        ctk.CTkButton(inner, text="Cambiar", width=90, height=36, command=pick).grid(row=0, column=1)

    def _behavior_section(self, parent, row: int):
        card = self._card(parent, "Comportamiento", row)
        self._switch_row(card, "Iniciar monitoreo automáticamente al abrir", "auto_start_monitoring", 2)
        self._switch_row(card, "Organizar archivos existentes al iniciar", "organize_on_start", 3)
        self._autostart_row(card, 4)
        ctk.CTkFrame(card, height=10, fg_color="transparent").grid(row=5)

    def _autostart_row(self, parent, grid_row: int):
        r = ctk.CTkFrame(parent, fg_color="transparent")
        r.grid(row=grid_row, column=0, padx=22, pady=8, sticky="ew")
        r.grid_columnconfigure(0, weight=1)
        ctk.CTkLabel(r, text="Iniciar con Windows", font=ctk.CTkFont(size=13)).grid(row=0, column=0, sticky="w")
        sw = ctk.CTkSwitch(r, text="")
        sw.grid(row=0, column=1)
        if get_autostart():
            sw.select()
        sw.configure(command=lambda s=sw: set_autostart(bool(s.get())))

    def _appearance_section(self, parent, row: int):
        card = self._card(parent, "Apariencia", row)
        r = ctk.CTkFrame(card, fg_color="transparent")
        r.grid(row=2, column=0, padx=22, pady=(0, 20), sticky="ew")
        r.grid_columnconfigure(0, weight=1)
        ctk.CTkLabel(r, text="Tema de la aplicación", font=ctk.CTkFont(size=13)).grid(row=0, column=0, sticky="w")

        combo = ctk.CTkComboBox(
            r, values=["dark", "light", "system"],
            width=140, state="readonly",
            command=lambda v: (self.config.set("theme", v), ctk.set_appearance_mode(v)),
        )
        combo.set(self.config.get("theme", "dark"))
        combo.grid(row=0, column=1)

def _danger_section(self, parent, row: int):
        card = self._card(parent, "Zona de riesgo", row)

        inner = ctk.CTkFrame(card, fg_color="transparent")
        inner.grid(row=2, column=0, padx=22, pady=(0, 20), sticky="ew")
        inner.grid_columnconfigure(0, weight=1)

        # Reset stats
        ctk.CTkLabel(
            inner,
            text="Reinicia el contador de estadísticas a cero.",
            font=ctk.CTkFont(size=12), text_color=("gray45", "gray55"),
        ).grid(row=0, column=0, sticky="w")
        ctk.CTkButton(
            inner, text="Restablecer estadísticas",
            width=190, height=36,
            fg_color=("#c0392b", "#7b241c"),
            hover_color=("#922b21", "#5c1b15"),
            command=self._reset_stats,
        ).grid(row=1, column=0, pady=(10, 0), sticky="w")

        # Separator
        ctk.CTkFrame(inner, height=1, fg_color=("gray80", "gray25")).grid(
            row=2, column=0, pady=(20, 16), sticky="ew"
        )

        # Reverse organization
        ctk.CTkLabel(
            inner,
            text="Devuelve todos los archivos organizados a la carpeta Descargas\n"
                 "y elimina las subcarpetas vacías.",
            font=ctk.CTkFont(size=12), text_color=("gray45", "gray55"),
            justify="left",
        ).grid(row=3, column=0, sticky="w")
        ctk.CTkButton(
            inner, text="Desorganizar archivos",
            width=190, height=36,
            fg_color=("#c0392b", "#7b241c"),
            hover_color=("#922b21", "#5c1b15"),
            command=self._reverse_organize,
        ).grid(row=4, column=0, pady=(10, 0), sticky="w")

        # Separator
        ctk.CTkFrame(inner, height=1, fg_color=("gray80", "gray25")).grid(
            row=5, column=0, pady=(20, 16), sticky="ew"
        )

        # Uninstall
        ctk.CTkLabel(
            inner,
            text="Elimina la configuración, los registros y el inicio automático con Windows.\n"
                 "Después podrás borrar el archivo .exe manualmente.",
            font=ctk.CTkFont(size=12), text_color=("gray45", "gray55"),
            justify="left",
        ).grid(row=6, column=0, sticky="w")
        ctk.CTkButton(
            inner, text="Desinstalar software",
            width=190, height=36,
            fg_color=("#7b241c", "#4a1010"),
            hover_color=("#5c1b15", "#350b0b"),
            command=self._uninstall,
        ).grid(row=7, column=0, pady=(10, 20), sticky="w")

    def _reset_stats(self):
        if messagebox.askyesno("Confirmar", "¿Deseas restablecer todas las estadísticas a cero?"):
            self.config.set("stats", {"total_moved": 0, "by_category": {}})
            messagebox.showinfo("Listo", "Estadísticas restablecidas correctamente.")

    def _reverse_organize(self):
        org = self.window.organizer
        if not messagebox.askyesno(
            "Desorganizar archivos",
            "Se moverán todos los archivos de las subcarpetas de vuelta a Descargas "
            "y se eliminarán las carpetas vacías.\n\n¿Continuar?",
            parent=self,
        ):
            return
        was_running = org.is_running
        if was_running:
            org.stop()
        org.reverse()
        if was_running:
            org.start()
        messagebox.showinfo(
            "Listo",
            "Restauración iniciada. Puedes ver el progreso en la pestaña Registros.",
            parent=self,
        )

    def _uninstall(self):
        if not messagebox.askyesno(
            "Desinstalar",
            "Se eliminarán:\n"
            "  • Configuración y registros de la aplicación\n"
            "  • Entrada de inicio automático con Windows\n\n"
            "El archivo .exe deberás borrarlo tú manualmente.\n\n"
            "¿Continuar con la desinstalación?",
            parent=self,
            icon="warning",
        ):
            return
        try:
            set_autostart(False)
        except Exception:
            pass
        try:
            if CONFIG_DIR.exists():
                shutil.rmtree(CONFIG_DIR)
        except Exception as exc:
            messagebox.showerror("Error", f"No se pudo eliminar la configuración:\n{exc}", parent=self)
            return
        messagebox.showinfo(
            "Desinstalado",
            "La configuración y el inicio automático han sido eliminados.\n\n"
            "Puedes borrar el archivo OrganizadorDescargas.exe para completar la desinstalación.",
            parent=self,
        )
        self.window._tray_quit()
