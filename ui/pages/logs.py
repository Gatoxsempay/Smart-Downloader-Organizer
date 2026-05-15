import os
from datetime import datetime
from pathlib import Path
import customtkinter as ctk

from core.config import CONFIG_DIR


class LogsPage(ctk.CTkFrame):
    def __init__(self, parent, window):
        super().__init__(parent, fg_color="transparent")
        self.window = window
        self._build()

    def _build(self):
        self.grid_columnconfigure(0, weight=1)
        self.grid_rowconfigure(1, weight=1)

        # ── header ────────────────────────────────────────────────────────────
        hdr = ctk.CTkFrame(self, fg_color="transparent")
        hdr.grid(row=0, column=0, padx=30, pady=(30, 10), sticky="ew")

        ctk.CTkLabel(
            hdr, text="Registros",
            font=ctk.CTkFont(size=24, weight="bold"),
        ).pack(side="left")

        ctk.CTkButton(
            hdr, text="Limpiar", width=86, height=34,
            fg_color=("gray82", "gray26"), text_color=("gray10", "gray90"),
            hover_color=("gray72", "gray34"),
            command=self._clear,
        ).pack(side="right")

        ctk.CTkButton(
            hdr, text="Abrir archivo", width=110, height=34,
            fg_color=("gray82", "gray26"), text_color=("gray10", "gray90"),
            hover_color=("gray72", "gray34"),
            command=self._open_file,
        ).pack(side="right", padx=(0, 8))

        # ── filter bar ───────────────────────────────────────────────────────
        fbar = ctk.CTkFrame(self, fg_color="transparent")
        fbar.grid(row=0, column=0, padx=30, pady=(84, 0), sticky="ew")

        self._filter = ctk.StringVar(value="Todos")
        for label in ("Todos", "Movidos", "Errores"):
            ctk.CTkRadioButton(
                fbar, text=label, variable=self._filter, value=label,
                font=ctk.CTkFont(size=12),
                command=self._apply_filter,
            ).pack(side="left", padx=(0, 16))

        # ── log box ───────────────────────────────────────────────────────────
        self._box = ctk.CTkTextbox(
            self, corner_radius=14,
            font=ctk.CTkFont(size=12, family="Courier New"),
            state="disabled", wrap="none",
        )
        self._box.grid(row=1, column=0, padx=30, pady=(4, 30), sticky="nsew")
        self._all_lines: list[tuple[str, str]] = []  # (text, tag)
        self._load_existing()

    def _load_existing(self):
        log_path = CONFIG_DIR / "organizador.log"
        if not log_path.exists():
            return
        try:
            lines = log_path.read_text(encoding="utf-8", errors="replace").splitlines()[-300:]
            for line in lines:
                tag = "ERROR" if "ERROR" in line else ("WARNING" if "WARNING" in line else "INFO")
                self._all_lines.append((line, tag))
            self._apply_filter()
        except Exception:
            pass

    def append_log(self, data: dict):
        msg = data.get("message", "")
        level = data.get("level", "INFO")
        now = datetime.now().strftime("%H:%M:%S")
        prefix = {"INFO": "ℹ", "WARNING": "⚠", "ERROR": "✖"}.get(level, "•")
        line = f"[{now}] {prefix}  {msg}"
        self._all_lines.append((line, level))
        if len(self._all_lines) > 1000:
            self._all_lines = self._all_lines[-800:]

        filt = self._filter.get()
        show = (
            filt == "Todos"
            or (filt == "Movidos" and "Movido:" in line)
            or (filt == "Errores" and level in ("ERROR", "WARNING"))
        )
        if show:
            self._write(line + "\n")

    def _apply_filter(self):
        filt = self._filter.get()
        self._box.configure(state="normal")
        self._box.delete("1.0", "end")
        self._box.configure(state="disabled")
        for line, tag in self._all_lines:
            if (
                filt == "Todos"
                or (filt == "Movidos" and "Movido:" in line)
                or (filt == "Errores" and tag in ("ERROR", "WARNING"))
            ):
                self._write(line + "\n")

    def _write(self, text: str):
        self._box.configure(state="normal")
        self._box.insert("end", text)
        self._box.see("end")
        self._box.configure(state="disabled")

    def _clear(self):
        self._all_lines.clear()
        self._box.configure(state="normal")
        self._box.delete("1.0", "end")
        self._box.configure(state="disabled")

    def _open_file(self):
        log_path = CONFIG_DIR / "organizador.log"
        if log_path.exists():
            os.startfile(str(log_path))
