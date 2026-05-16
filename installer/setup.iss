#ifndef AppVersion
  #define AppVersion "2.3.0"
#endif

#define AppName      "Organizador de Descargas"
#define AppPublisher "Gatoxsempay"
#define AppExeName   "OrganizadorDescargas.exe"
#define AppURL       "https://github.com/Gatoxsempay/Smart-Downloader-Organizer"

[Setup]
AppId={{B3A7C94D-2F1E-4B8A-9C5D-E6F7A0B1C2D3}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} v{#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases
VersionInfoVersion={#AppVersion}

; Instalación por usuario (no requiere permisos de administrador)
; El usuario puede optar por instalar para todos los usuarios desde el diálogo
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
AllowNoIcons=yes
DisableDirPage=no

; Salida
OutputDir=..\release
OutputBaseFilename=OrganizadorDescargas-Setup-v{#AppVersion}
SetupIconFile=..\Assets\AppIcon.ico

; Compresión
Compression=lzma2
SolidCompression=yes

; Apariencia
WizardStyle=modern

; Requisitos mínimos del sistema
MinVersion=10.0.17763

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[CustomMessages]
spanish.OptGroupTitle=Opciones de instalación

[Tasks]
Name: "desktopicon"; \
  Description: "Crear acceso directo en el &escritorio"; \
  GroupDescription: "{cm:OptGroupTitle}"; \
  Flags: unchecked

Name: "autostart"; \
  Description: "Iniciar automáticamente con &Windows"; \
  GroupDescription: "{cm:OptGroupTitle}"; \
  Flags: unchecked

Name: "launchapp"; \
  Description: "Iniciar {#AppName} al terminar la instalación"

[Files]
Source: "..\publish\win-x64\*"; \
  DestDir: "{app}"; \
  Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Acceso directo en el menú Inicio
Name: "{group}\{#AppName}";            Filename: "{app}\{#AppExeName}"
Name: "{group}\Desinstalar {#AppName}"; Filename: "{uninstallexe}"

; Acceso directo en el escritorio (opcional)
Name: "{autodesktop}\{#AppName}"; \
  Filename: "{app}\{#AppExeName}"; \
  Tasks: desktopicon

[Registry]
; Inicio automático con Windows (opcional, misma clave que usa la app internamente)
Root: HKCU; \
  Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; \
  ValueName: "OrganizadorDescargas"; \
  ValueData: """{app}\{#AppExeName}"""; \
  Flags: uninsdeletevalue; \
  Tasks: autostart

[Run]
; Lanzar la app al terminar el instalador (opcional)
Filename: "{app}\{#AppExeName}"; \
  Description: "Iniciar {#AppName}"; \
  Flags: nowait postinstall skipifsilent; \
  Tasks: launchapp

[UninstallRun]
; Cerrar la app antes de desinstalar
Filename: "{cmd}"; \
  Parameters: "/c taskkill /f /im {#AppExeName}"; \
  RunOnceId: "KillApp"; \
  Flags: runhidden skipifdoesntexist

[UninstallDelete]
; Eliminar carpeta de configuración al desinstalar
Type: filesandordirs; Name: "{userappdata}\OrganizadorDescargas"
Type: dirifempty;     Name: "{app}"
