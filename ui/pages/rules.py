import customtkinter as ctk
from tkinter import messagebox


class RulesPage(ctk.CTkFrame):
    def __init__(self, parent, window):
        super().__init__(parent, fg_color="transparent")
        self.window = window
        self.config = window.config
        self._rendered_version = -1
        self._build()

    # ── layout skeleton (built once) ──────────────────────────────────────────

    def _build(self):
        self.grid_columnconfigure(0, weight=1)
        self.grid_rowconfigure(2, weight=1)

        # Header
        hdr = ctk.CTkFrame(self, fg_color="transparent")
        hdr.grid(row=0, column=0, padx=30, pady=(30, 6), sticky="ew")

        ctk.CTkLabel(
            hdr, text="Reglas por carpeta",
            font=ctk.CTkFont(size=24, weight="bold"),
        ).pack(side="left")

        ctk.CTkButton(
            hdr, text="+ Nueva carpeta", width=145, height=36,
            command=self._new_folder_dialog,
        ).pack(side="right")

        # Search + count
        sub = ctk.CTkFrame(self, fg_color="transparent")
        sub.grid(row=1, column=0, padx=30, pady=(0, 8), sticky="ew")

        self._search_var = ctk.StringVar()
        self._search_var.trace_add("write", lambda *_: self._refresh())
        ctk.CTkEntry(
            sub, textvariable=self._search_var,
            placeholder_text="Buscar carpeta o extensión...",
            height=34, width=270,
        ).pack(side="left")

        self._count_lbl = ctk.CTkLabel(
            sub, text="",
            font=ctk.CTkFont(size=12), text_color=("gray50", "gray55"),
        )
        self._count_lbl.pack(side="left", padx=14)

        # Scrollable list of folder cards
        self._list = ctk.CTkScrollableFrame(self, corner_radius=12)
        self._list.grid(row=2, column=0, padx=30, pady=(0, 30), sticky="nsew")
        self._list.grid_columnconfigure(0, weight=1)

    # ── data helpers ──────────────────────────────────────────────────────────

    def _grouped(self) -> dict[str, list[str]]:
        """Returns {folder: [sorted extensions]} filtered by search query."""
        rules = self.config.get("extension_rules", {})
        q = self._search_var.get().lower()
        groups: dict[str, list[str]] = {}
        for ext, folder in rules.items():
            if q and q not in folder.lower() and q not in ext.lower():
                continue
            groups.setdefault(folder, []).append(ext)
        return {f: sorted(exts) for f, exts in sorted(groups.items())}

    # ── refresh (only when data changed) ─────────────────────────────────────

    def _refresh(self):
        for w in self._list.winfo_children():
            w.destroy()

        groups = self._grouped()
        total_ext = sum(len(v) for v in groups.values())
        self._count_lbl.configure(
            text=f"{len(groups)} carpeta(s)  ·  {total_ext} extensión(es)"
        )

        if not groups:
            ctk.CTkLabel(
                self._list,
                text="Sin resultados. Agrega una nueva carpeta con el botón superior.",
                text_color=("gray50", "gray55"),
                font=ctk.CTkFont(size=13),
            ).pack(pady=40)
        else:
            for folder, exts in groups.items():
                self._folder_card(folder, exts)

        self._rendered_version = self.config.version

    def on_show(self):
        if self.config.version != self._rendered_version:
            self._refresh()

    # ── folder card ───────────────────────────────────────────────────────────

    def _folder_card(self, folder: str, exts: list[str]):
        card = ctk.CTkFrame(self._list, corner_radius=12)
        card.pack(fill="x", pady=6, padx=4)
        card.grid_columnconfigure(0, weight=1)

        # Top row: folder name + action buttons
        top = ctk.CTkFrame(card, fg_color="transparent")
        top.grid(row=0, column=0, padx=18, pady=(14, 8), sticky="ew")
        top.grid_columnconfigure(0, weight=1)

        ctk.CTkLabel(
            top, text=f"  {folder}",
            font=ctk.CTkFont(size=15, weight="bold"),
        ).grid(row=0, column=0, sticky="w")

        badge = ctk.CTkLabel(
            top,
            text=f"{len(exts)} ext.",
            font=ctk.CTkFont(size=11),
            text_color=("gray50", "gray55"),
        )
        badge.grid(row=0, column=1, padx=(8, 16))

        ctk.CTkButton(
            top, text="Gestionar extensiones", width=170, height=30,
            fg_color=("gray82", "gray26"), text_color=("gray10", "gray90"),
            hover_color=("gray72", "gray34"),
            font=ctk.CTkFont(size=12),
            command=lambda f=folder, e=exts: self._manage_dialog(f, e),
        ).grid(row=0, column=2, padx=(0, 6))

        ctk.CTkButton(
            top, text="Eliminar carpeta", width=130, height=30,
            fg_color="transparent",
            border_width=1, border_color=("gray70", "gray36"),
            text_color=("#c0392b", "#e05555"),
            hover_color=("#fde8e8", "#3a0f0f"),
            font=ctk.CTkFont(size=12),
            command=lambda f=folder: self._delete_folder(f),
        ).grid(row=0, column=3)

        # Divider
        ctk.CTkFrame(card, height=1, fg_color=("gray82", "gray22")).grid(
            row=1, column=0, padx=18, sticky="ew"
        )

        # Extension chips preview
        chips_frame = ctk.CTkFrame(card, fg_color="transparent")
        chips_frame.grid(row=2, column=0, padx=14, pady=(8, 14), sticky="ew")

        show_exts = exts[:12]
        for ext in show_exts:
            chip = ctk.CTkLabel(
                chips_frame,
                text=f" {ext} ",
                font=ctk.CTkFont(size=12, family="Courier New"),
                fg_color=("gray84", "gray24"),
                corner_radius=5,
                text_color=("gray15", "gray85"),
            )
            chip.pack(side="left", padx=3, pady=2)

        if len(exts) > 12:
            ctk.CTkLabel(
                chips_frame,
                text=f"  +{len(exts) - 12} más",
                font=ctk.CTkFont(size=12),
                text_color=("gray50", "gray55"),
            ).pack(side="left", padx=4)

    # ── manage extensions dialog ──────────────────────────────────────────────

    def _manage_dialog(self, folder: str, exts: list[str]):
        dlg = ctk.CTkToplevel(self)
        dlg.title(f"Extensiones — {folder}")
        dlg.geometry("480x520")
        dlg.resizable(False, True)
        dlg.grab_set()
        dlg.lift()
        dlg.focus()
        dlg.grid_columnconfigure(0, weight=1)
        dlg.grid_rowconfigure(2, weight=1)

        ctk.CTkLabel(
            dlg, text=folder,
            font=ctk.CTkFont(size=17, weight="bold"),
        ).grid(row=0, column=0, padx=24, pady=(22, 4), sticky="w")

        ctk.CTkLabel(
            dlg,
            text="Haz clic en ✕ para eliminar una extensión de esta carpeta.",
            font=ctk.CTkFont(size=12), text_color=("gray45", "gray55"),
        ).grid(row=1, column=0, padx=24, pady=(0, 10), sticky="w")

        # Scrollable extension list
        ext_scroll = ctk.CTkScrollableFrame(dlg, corner_radius=10)
        ext_scroll.grid(row=2, column=0, padx=20, pady=(0, 10), sticky="nsew")
        ext_scroll.grid_columnconfigure(0, weight=1)

        def rebuild_list():
            for w in ext_scroll.winfo_children():
                w.destroy()
            current = self._exts_for_folder(folder)
            if not current:
                ctk.CTkLabel(
                    ext_scroll, text="(sin extensiones — esta carpeta será eliminada)",
                    text_color=("gray50", "gray55"), font=ctk.CTkFont(size=12),
                ).pack(pady=20)
                return
            for ext in sorted(current):
                row = ctk.CTkFrame(ext_scroll, corner_radius=8, height=40)
                row.pack(fill="x", pady=2)
                row.pack_propagate(False)
                ctk.CTkLabel(
                    row, text=ext, anchor="w",
                    font=ctk.CTkFont(size=13, family="Courier New"),
                ).pack(side="left", padx=14, fill="x", expand=True)
                ctk.CTkButton(
                    row, text="✕", width=30, height=28,
                    fg_color="transparent",
                    text_color=("#c0392b", "#e05555"),
                    hover_color=("#fde8e8", "#3a0f0f"),
                    command=lambda e=ext: remove_ext(e),
                ).pack(side="right", padx=8)

        def remove_ext(ext: str):
            rules = dict(self.config.get("extension_rules", {}))
            rules.pop(ext, None)
            self.config.set("extension_rules", rules)
            rebuild_list()
            self._refresh()

        rebuild_list()

        # Divider
        ctk.CTkFrame(dlg, height=1, fg_color=("gray80", "gray25")).grid(
            row=3, column=0, padx=20, pady=(0, 10), sticky="ew"
        )

        # Add extensions row
        ctk.CTkLabel(
            dlg, text="Agregar extensiones (separadas por comas):",
            font=ctk.CTkFont(size=13),
        ).grid(row=4, column=0, padx=24, pady=(0, 4), sticky="w")

        add_row = ctk.CTkFrame(dlg, fg_color="transparent")
        add_row.grid(row=5, column=0, padx=20, pady=(0, 20), sticky="ew")
        add_row.grid_columnconfigure(0, weight=1)

        add_entry = ctk.CTkEntry(
            add_row,
            placeholder_text=".mcaddon, .mcpack, .mcworld",
            height=36,
        )
        add_entry.grid(row=0, column=0, sticky="ew", padx=(0, 10))

        def add_exts():
            raw = add_entry.get()
            new_exts = [
                e.strip().lower()
                for e in raw.split(",")
                if e.strip().startswith(".")
            ]
            if not new_exts:
                messagebox.showwarning(
                    "Error",
                    "Introduce extensiones válidas separadas por comas (ej: .mcaddon, .mcpack)",
                    parent=dlg,
                )
                return
            rules = dict(self.config.get("extension_rules", {}))
            for ext in new_exts:
                rules[ext] = folder
            self.config.set("extension_rules", rules)
            add_entry.delete(0, "end")
            rebuild_list()
            self._refresh()

        ctk.CTkButton(add_row, text="Agregar", width=90, height=36, command=add_exts).grid(row=0, column=1)
        add_entry.bind("<Return>", lambda _: add_exts())

    def _exts_for_folder(self, folder: str) -> list[str]:
        rules = self.config.get("extension_rules", {})
        return [ext for ext, f in rules.items() if f == folder]

    # ── new folder dialog ─────────────────────────────────────────────────────

    def _new_folder_dialog(self):
        dlg = ctk.CTkToplevel(self)
        dlg.title("Nueva carpeta")
        dlg.geometry("440x250")
        dlg.resizable(False, False)
        dlg.grab_set()
        dlg.lift()
        dlg.focus()

        ctk.CTkLabel(
            dlg, text="Nombre de la carpeta:",
            font=ctk.CTkFont(size=13),
        ).pack(padx=28, pady=(26, 4), anchor="w")
        folder_entry = ctk.CTkEntry(dlg, placeholder_text="Mis Mods", height=36)
        folder_entry.pack(padx=28, fill="x")

        ctk.CTkLabel(
            dlg, text="Extensiones (separadas por comas):",
            font=ctk.CTkFont(size=13),
        ).pack(padx=28, pady=(16, 4), anchor="w")
        ext_entry = ctk.CTkEntry(
            dlg, placeholder_text=".mcaddon, .mcpack, .mcworld", height=36
        )
        ext_entry.pack(padx=28, fill="x")

        def save():
            folder = folder_entry.get().strip()
            if not folder:
                messagebox.showwarning("Error", "El nombre de carpeta no puede estar vacío.", parent=dlg)
                return
            new_exts = [
                e.strip().lower()
                for e in ext_entry.get().split(",")
                if e.strip().startswith(".")
            ]
            if not new_exts:
                messagebox.showwarning(
                    "Error",
                    "Introduce al menos una extensión válida (ej: .mcaddon)",
                    parent=dlg,
                )
                return
            rules = dict(self.config.get("extension_rules", {}))
            for ext in new_exts:
                rules[ext] = folder
            self.config.set("extension_rules", rules)
            self._refresh()
            dlg.destroy()

        ctk.CTkButton(dlg, text="Crear carpeta", height=38, command=save).pack(
            pady=22, padx=28, fill="x"
        )
        folder_entry.bind("<Return>", lambda _: ext_entry.focus())
        ext_entry.bind("<Return>", lambda _: save())

    # ── delete entire folder group ────────────────────────────────────────────

    def _delete_folder(self, folder: str):
        exts = self._exts_for_folder(folder)
        if not messagebox.askyesno(
            "Eliminar carpeta",
            f"¿Eliminar la carpeta '{folder}' y sus {len(exts)} extensión(es)?\n\n"
            + ", ".join(sorted(exts)),
        ):
            return
        rules = dict(self.config.get("extension_rules", {}))
        for ext in exts:
            rules.pop(ext, None)
        self.config.set("extension_rules", rules)
        self._refresh()
