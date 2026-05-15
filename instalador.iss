; =========================================================
; Inno Setup Script - Organizador Inteligente de Descargas
; Copyright (c) 2026 Fernando Alonso Maldonado Rivero
; =========================================================

#define AppName      "Organizador Inteligente de Descargas"
#define AppVersion   "1.0"
#define AppPublisher "Fernando Alonso Maldonado Rivero"
#define AppExeName   "iniciar_organizador.bat"

[Setup]
AppId={{A3F2C8D1-5E4B-4F9A-B2C7-8D3E6F1A0B5C}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://creativecommons.org/licenses/by-nc-nd/4.0/deed.es
AppSupportURL=https://creativecommons.org/licenses/by-nc-nd/4.0/deed.es
LicenseFile=LICENSE.txt

; Instalar solo para el usuario actual (no requiere administrador)
DefaultDirName={userappdata}\OrganizadorDescargas
DefaultGroupName={#AppName}
DisableProgramGroupPage=no
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; Salida
OutputDir=Instalador
OutputBaseFilename=OrganizadorDescargas_Setup_v1.0
SetupIconFile=
Compression=lzma2/ultra64
SolidCompression=yes

; Apariencia
WizardStyle=modern
WizardResizable=no
DisableWelcomePage=no
ShowLanguageDialog=no

; Desinstalador
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\icono.ico
CreateUninstallRegKey=yes

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Messages]
; Mensajes personalizados en español
WelcomeLabel1=Bienvenido al instalador de{%n}{#AppName}
WelcomeLabel2=Este asistente instalará {#AppName} versión {#AppVersion} en tu equipo.{%n}{%n}El programa organizará automáticamente tu carpeta Descargas clasificando los archivos por tipo.{%n}{%n}Haz clic en Siguiente para continuar.
FinishedHeadingLabel=Instalación completada
FinishedLabel=La instalación de {#AppName} ha finalizado correctamente.{%n}{%n}El organizador se iniciará automáticamente con Windows.

[Files]
; Archivos principales del programa
Source: "organizador_descargas.py";  DestDir: "{app}"; Flags: ignoreversion
Source: "organizador_descargas.pyw"; DestDir: "{app}"; Flags: ignoreversion
Source: "iniciar_organizador.bat";   DestDir: "{app}"; Flags: ignoreversion
Source: "iniciar_organizador.ps1";   DestDir: "{app}"; Flags: ignoreversion
Source: "LICENSE.txt";               DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Acceso directo en el escritorio
Name: "{userdesktop}\Organizador de Descargas"; \
      Filename: "{app}\{#AppExeName}"; \
      Comment: "Inicia el Organizador Inteligente de Descargas"; \
      IconFilename: "{sys}\shell32.dll"; IconIndex: 44

; Acceso directo en el menú Inicio
Name: "{group}\Organizador de Descargas"; \
      Filename: "{app}\{#AppExeName}"; \
      Comment: "Inicia el Organizador Inteligente de Descargas"; \
      IconFilename: "{sys}\shell32.dll"; IconIndex: 44

; Desinstalador en el menú Inicio
Name: "{group}\Desinstalar {#AppName}"; Filename: "{uninstallexe}"

[Registry]
; Inicio automático con Windows
Root: HKCU; \
     Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; \
     ValueType: string; \
     ValueName: "OrganizadorDescargas"; \
     ValueData: """{app}\{#AppExeName}"""; \
     Flags: uninsdeletevalue

[Run]
; Instalar dependencia watchdog si no está presente
Filename: "cmd.exe"; \
          Parameters: "/c pip install watchdog --quiet"; \
          Flags: runhidden waituntilterminated; \
          StatusMsg: "Instalando dependencias (watchdog)..."; \
          Description: "Instalar dependencia watchdog"

; Iniciar el programa al terminar la instalación
Filename: "{app}\{#AppExeName}"; \
          Description: "Iniciar el Organizador de Descargas ahora"; \
          Flags: nowait postinstall skipifsilent

[UninstallRun]
; Detener el proceso antes de desinstalar
Filename: "taskkill"; Parameters: "/f /im pythonw.exe"; Flags: runhidden; RunOnceId: "KillPythonw"

[Code]
// Verificar que Python esté instalado antes de instalar
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;

  // Intentar ejecutar python --version
  if not Exec('cmd.exe', '/c python --version', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) or (ResultCode <> 0) then
  begin
    // Intentar con el launcher py.exe
    if not Exec('cmd.exe', '/c py --version', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) or (ResultCode <> 0) then
    begin
      if MsgBox(
        'No se detectó Python en este equipo.' + #13#10 + #13#10 +
        'El Organizador de Descargas requiere Python 3.8 o superior.' + #13#10 +
        'Puedes instalarlo desde la Microsoft Store buscando "Python".' + #13#10 + #13#10 +
        '¿Deseas continuar de todas formas?',
        mbConfirmation, MB_YESNO) = IDNO then
      begin
        Result := False;
      end;
    end;
  end;
end;
