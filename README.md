# Organizador de PC · v3.0.0

> Aplicación de escritorio para Windows que mantiene tu PC organizado de forma automática e inteligente.

![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![Framework](https://img.shields.io/badge/framework-.NET%2010%20%7C%20WinUI%203-purple)
![Version](https://img.shields.io/badge/version-3.0.0-green)
![License](https://img.shields.io/badge/license-MIT-orange)

---

## ¿Qué hace?

**Organizador de PC** es una herramienta todo-en-uno que organiza automáticamente tus archivos, limpia el escritorio, detecta duplicados y te permite ejecutar acciones frecuentes con un solo clic — sin necesidad de conocimientos técnicos.

---

## Características principales

### Panel de control
Vista rápida del estado del organizador, archivos movidos recientemente y actividad en tiempo real.

### Organizador de Carpetas
Monitorea carpetas (Descargas, Documentos, etc.) y mueve los archivos automáticamente según su tipo:
- Imágenes, Vídeos, Música, Documentos, Programas, Archivos comprimidos, Código, y más
- Reglas personalizables con extensiones y carpetas de destino propias
- Modo manual o automático con vigilancia en segundo plano

### Organizador de Escritorio
Ordena los archivos del escritorio en subcarpetas por categoría con un solo clic. Permite deshacer la organización y restaurar el estado original.

### Explorador Avanzado
Explorador de archivos integrado con:
- Árbol de navegación por unidades y carpetas
- Vista de archivos con metadatos (tipo, tamaño, fecha, aperturas)
- Filtrado y ordenación por cualquier columna
- Detección de archivos más usados
- Navegación con historial (← →)
- Modo recursivo (incluir subcarpetas)

### Acciones Rápidas (Macros)
Crea secuencias de acciones que se ejecutan con un clic, sin necesidad de programar:
- Abrir carpeta, programa o sitio web
- Limpiar archivos temporales
- Vaciar la papelera de reciclaje
- Apagar, reiniciar, suspender o bloquear el equipo
- Combinar múltiples acciones en una sola macro

### Detector de Duplicados
Encuentra archivos duplicados por contenido (hash SHA-256) con opciones para eliminar o mover los duplicados.

### Archivos Fantasma
Detecta archivos que nunca han sido abiertos y llevan mucho tiempo ocupando espacio, con opción de eliminarlos.

### Barra Portátil
Mini-barra flotante siempre visible (o auto-colapsable) con accesos directos a tus macros y aplicaciones favoritas. Se arrastra libremente por la pantalla.

### Reglas y Exclusiones
- Define reglas de organización personalizadas (extensión → carpeta)
- Excluye carpetas específicas del monitoreo automático

### Bandeja del sistema
La aplicación se minimiza al área de notificación para no ocupar espacio en la barra de tareas. Acceso instantáneo con clic derecho.

---

## Capturas de pantalla

> *(próximamente)*

---

## Requisitos

| Requisito | Mínimo |
|-----------|--------|
| Sistema operativo | Windows 10 versión 1809 (build 17763) o superior |
| Arquitectura | x64 |
| .NET Runtime | Incluido (self-contained) |

---

## Instalación

### Opción A — Instalador (recomendado)
1. Descarga `OrganizadorDescargas-Setup-v3.0.0.exe` desde [Releases](../../releases)
2. Ejecuta el instalador y sigue los pasos
3. Opcional: crea acceso directo en el escritorio e inicio automático con Windows

### Opción B — Portable (sin instalador)
1. Descarga el ZIP desde Releases
2. Extrae en cualquier carpeta
3. Ejecuta `OrganizadorDescargas.exe`

---

## Primeros pasos

1. Abre la app — aparece en la bandeja del sistema
2. Ve a **Carpetas** y añade las carpetas que quieres organizar
3. Activa el monitoreo con el botón **Iniciar**
4. Configura las **Reglas** y **Exclusiones** según tus preferencias
5. Crea tus propias **Acciones Rápidas** para tareas frecuentes

---

## Tecnología

- **Lenguaje:** C# 13
- **Framework:** .NET 10 · Windows App SDK 2.0 · WinUI 3
- **Instalador:** Inno Setup 6
- **Modo:** Unpackaged (sin identidad de paquete)
- **Distribución:** Self-contained (no requiere instalar .NET por separado)

---

## Licencia

MIT © 2026 [Gatoxsempay](https://github.com/Gatoxsempay)
