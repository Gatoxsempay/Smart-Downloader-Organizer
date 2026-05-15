from datetime import datetime
import customtkinter as ctk


class DashboardPage(ctk.CTkFrame):
    def __init__(self, parent, window):
        super().__init__(parent, fg_color="transparent")
        self.window = window
        self.config = window.config
        self._is_running = False
        self._today = 0
        self._build()

    def _build(self):
        self.grid_columnconfigure(0, weight=1)
        self.grid_rowconfigure(3, weight=1)

        # ── header ────────────────────────────────────────────────────────────
        hdr = ctk.CTkFrame(self, fg_color="transparent")
        hdr.grid(row=0, column=0, padx=30, pady=(30, 0), sticky="ew")

        ctk.CTkLabel(
            hdr, text="Panel principal",
            font=ctk.CTkFont(size=24, weight="bold"),
        ).pack(side="left")

        self._ctrl_btn = ctk.CTkButton(
            hdr, text="Iniciar", width=120, height=38,
            font=ctk.CTkFont(size=13, weight="bold"),
            fg_color="#1f6aa5", hover_color="#164f7a",
            command=self._toggle,
        )
        self._ctrl_btn.pack(side="right")

        ctk.CTkButton(
            hdr, text="Organizar ahora", width=140, height=38,
            font=ctk.CTkFont(size=13),
            fg_color=("gray82", "gray26"), text_color=("gray10", "gray90"),
            hover_color=("gray72", "gray34"),
            command=self.window.organizer.organize_now,
        ).pack(side="right", padx=(0, 10))

        # ── status + stats card ───────────────────────────────────────────────
        card = ctk.CTkFrame(self, corner_radius=14)
        card.grid(row=1, column=0, padx=30, pady=22, sticky="ew")
        card.grid_columnconfigure((1, 2, 3), weight=1)

        self._status_lbl = ctk.CTkLabel(
            card, text="  Detenido",
            font=ctk.CTkFont(size=15, weight="bold"),
            text_color=("gray55", "gray50"),
        )
        self._status_lbl.grid(row=0, column=0, padx=(20, 30), pady=20, sticky="w")

        stats = self.config.get("stats", {})
        self._lbl_total = self._stat_block(card, str(stats.get("total_moved", 0)), "Total movidos", 1)
        self._lbl_today = self._stat_block(card, "0", "Hoy", 2)
        self._lbl_cats  = self._stat_block(card, str(len(stats.get("by_category", {}))), "Categorías", 3)

        # ── folder path display ───────────────────────────────────────────────
        path_card = ctk.CTkFrame(self, corner_radius=10, fg_color=("gray88", "gray17"))
        path_card.grid(row=2, column=0, padx=30, pady=(0, 18), sticky="ew")

        ctk.CTkLabel(
            path_card, text="Monitoreando:",
            font=ctk.CTkFont(size=12), text_color=("gray45", "gray55"),
        ).pack(side="left", padx=(16, 6), pady=10)

        self._path_lbl = ctk.CTkLabel(
            path_card,
            text=self.config.get("monitored_folder", ""),
            font=ctk.CTkFont(size=12, family="Courier"),
            text_color=("gray20", "gray85"),
        )
        self._path_lbl.pack(side="left", pady=10)

        # ── recent activity ───────────────────────────────────────────────────
        ctk.CTkLabel(
            self, text="Actividad reciente",
            font=ctk.CTkFont(size=15, weight="bold"),
        ).grid(row=3, column=0, padx=30, pady=(0, 6), sticky="nw")

        self._feed = ctk.CTkScrollableFrame(self, corner_radius=14)
        self._feed.grid(row=4, column=0, padx=30, pady=(0, 30), sticky="nsew")
        self._feed.grid_columnconfigure(0, weight=1)
        self.grid_rowconfigure(4, weight=1)
        self._rows: list = []

        self._empty_lbl = ctk.CTkLabel(
            self._feed,
            text="Sin actividad aún — inicia el organizador para comenzar.",
            text_color=("gray55", "gray50"),
            font=ctk.CTkFont(size=12),
        )
        self._empty_lbl.pack(pady=30)

    @staticmethod
    def _stat_block(parent, value, label, col):
        f = ctk.CTkFrame(parent, fg_color="transparent")
        f.grid(row=0, column=col, padx=20, pady=18)
        val = ctk.CTkLabel(f, text=value, font=ctk.CTkFont(size=30, weight="bold"))
        val.pack()
        ctk.CTkLabel(
            f, text=label,
            font=ctk.CTkFont(size=11),
            text_color=("gray45", "gray55"),
        ).pack()
        return val

    # ── public API (called by MainWindow) ─────────────────────────────────────

    def update_status(self, status: str):
        self._is_running = status == "running"
        if self._is_running:
            self._status_lbl.configure(text="  Activo", text_color="#2CC985")
            self._ctrl_btn.configure(text="Detener", fg_color="#c0392b", hover_color="#922b21")
        else:
            self._status_lbl.configure(text="  Detenido", text_color=("gray55", "gray50"))
            self._ctrl_btn.configure(text="Iniciar", fg_color="#1f6aa5", hover_color="#164f7a")

    def on_file_moved(self, _data):
        self._today += 1
        self._lbl_today.configure(text=str(self._today))
        stats = self.config.get("stats", {})
        self._lbl_total.configure(text=str(stats.get("total_moved", 0)))
        cats = stats.get("by_category", {})
        self._lbl_cats.configure(text=str(len(cats)))

    def append_activity(self, data: dict):
        msg = data.get("message", "")
        if "Movido:" not in msg:
            return
        if self._empty_lbl.winfo_exists():
            try:
                self._empty_lbl.destroy()
            except Exception:
                pass

        now = datetime.now().strftime("%H:%M")
        row = ctk.CTkFrame(self._feed, corner_radius=8, height=42)
        row.pack(fill="x", pady=2, padx=4)
        row.pack_propagate(False)

        ctk.CTkLabel(
            row, text=now, width=46,
            font=ctk.CTkFont(size=11),
            text_color=("gray50", "gray55"),
        ).pack(side="left", padx=(12, 2))

        ctk.CTkLabel(
            row, text=msg, anchor="w",
            font=ctk.CTkFont(size=12),
        ).pack(side="left", fill="x", expand=True, padx=4)

        self._rows.append(row)
        if len(self._rows) > 60:
            self._rows.pop(0).destroy()

    def on_show(self):
        self._path_lbl.configure(text=self.config.get("monitored_folder", ""))

    def _toggle(self):
        if self._is_running:
            self.window.organizer.stop()
        else:
            self.window.organizer.start()
