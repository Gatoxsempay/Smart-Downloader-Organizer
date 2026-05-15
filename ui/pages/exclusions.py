import customtkinter as ctk
from tkinter import messagebox


class ExclusionsPage(ctk.CTkFrame):
    def __init__(self, parent, window):
        super().__init__(parent, fg_color="transparent")
        self.window = window
        self.config = window.config
        self._rendered_version = -1
        self._build()

    def on_show(self):
        # Re-render only if config changed since last visit
        if self.config.version != self._rendered_version:
            self._rendered_version = self.config.version

    def _build(self):
        self.grid_columnconfigure((0, 1), weight=1)
        self.grid_rowconfigure(2, weight=1)

        ctk.CTkLabel(
            self, text="Exclusiones",
            font=ctk.CTkFont(size=24, weight="bold"),
        ).grid(row=0, column=0, columnspan=2, padx=30, pady=(30, 6), sticky="w")

        ctk.CTkLabel(
            self,
            text="Los archivos que coincidan con estas reglas no serán movidos.",
            font=ctk.CTkFont(size=13), text_color=("gray45", "gray55"),
        ).grid(row=1, column=0, columnspan=2, padx=30, pady=(0, 18), sticky="w")

        self._panel(
            col=0, title="Extensiones ignoradas",
            desc="El archivo entero no se mueve si tiene esta extensión.",
            key="excluded_extensions", placeholder=".crdownload",
            left_pad=30, right_pad=8,
        )
        self._panel(
            col=1, title="Nombres de archivo ignorados",
            desc="Archivos con este nombre exacto no se mueven.",
            key="excluded_names", placeholder="desktop.ini",
            left_pad=8, right_pad=30,
        )

    def _panel(self, col, title, desc, key, placeholder, left_pad, right_pad):
        frame = ctk.CTkFrame(self, corner_radius=14)
        frame.grid(row=2, column=col, padx=(left_pad, right_pad), pady=(0, 30), sticky="nsew")
        frame.grid_columnconfigure(0, weight=1)
        frame.grid_rowconfigure(3, weight=1)

        ctk.CTkLabel(
            frame, text=title,
            font=ctk.CTkFont(size=15, weight="bold"),
        ).grid(row=0, column=0, padx=20, pady=(20, 4), sticky="w")

        ctk.CTkLabel(
            frame, text=desc, wraplength=260, justify="left",
            font=ctk.CTkFont(size=12), text_color=("gray45", "gray55"),
        ).grid(row=1, column=0, padx=20, pady=(0, 14), sticky="w")

        # add row
        add_row = ctk.CTkFrame(frame, fg_color="transparent")
        add_row.grid(row=2, column=0, padx=16, pady=(0, 8), sticky="ew")
        add_row.grid_columnconfigure(0, weight=1)

        entry = ctk.CTkEntry(add_row, placeholder_text=placeholder, height=34)
        entry.grid(row=0, column=0, sticky="ew", padx=(0, 8))

        list_frame = ctk.CTkScrollableFrame(frame, corner_radius=10)
        list_frame.grid(row=3, column=0, padx=16, pady=(0, 16), sticky="nsew")
        list_frame.grid_columnconfigure(0, weight=1)

        def refresh():
            for w in list_frame.winfo_children():
                w.destroy()
            items = self.config.get(key, [])
            if not items:
                ctk.CTkLabel(
                    list_frame, text="(vacío)",
                    text_color=("gray55", "gray50"),
                    font=ctk.CTkFont(size=12),
                ).pack(pady=20)
                return
            for item in items:
                r = ctk.CTkFrame(list_frame, corner_radius=7, height=38)
                r.pack(fill="x", pady=2)
                r.pack_propagate(False)
                ctk.CTkLabel(
                    r, text=item, anchor="w",
                    font=ctk.CTkFont(size=13, family="Courier"),
                ).pack(side="left", padx=12, fill="x", expand=True)
                ctk.CTkButton(
                    r, text="✕", width=28, height=26,
                    fg_color="transparent",
                    text_color=("#c0392b", "#e74c3c"),
                    hover_color=("#fde8e8", "#3a0f0f"),
                    command=lambda i=item: remove(i),
                ).pack(side="right", padx=6)

        def add():
            val = entry.get().strip().lower()
            if not val:
                return
            items = list(self.config.get(key, []))
            if val in items:
                messagebox.showinfo("Duplicado", f"'{val}' ya existe en la lista.", parent=self)
                return
            items.append(val)
            self.config.set(key, items)
            entry.delete(0, "end")
            refresh()

        def remove(item):
            items = list(self.config.get(key, []))
            if item in items:
                items.remove(item)
                self.config.set(key, items)
                refresh()

        add_btn = ctk.CTkButton(add_row, text="Agregar", width=80, height=34, command=add)
        add_btn.grid(row=0, column=1)
        entry.bind("<Return>", lambda _: add())
        refresh()
