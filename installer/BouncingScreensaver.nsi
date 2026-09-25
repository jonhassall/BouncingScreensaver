Unicode true
RequestExecutionLevel admin
SetRegView 64

!include "MUI2.nsh"
!include "x64.nsh"

!ifndef VERSION
  !define VERSION "0.1.0"
!endif
!ifndef SCR_PATH
  !error "SCR_PATH must be supplied to makensis."
!endif
!ifndef OUTPUT_DIR
  !define OUTPUT_DIR "."
!endif

!define PRODUCT_NAME "BouncingScreensaver"
!define PRODUCT_PUBLISHER "Your Company"
!define PRODUCT_KEY "Software\BouncingScreensaver"
!define UNINSTALL_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\BouncingScreensaver"

Name "${PRODUCT_NAME}"
OutFile "${OUTPUT_DIR}\BouncingScreensaver-Setup.exe"
InstallDir "$PROGRAMFILES64\BouncingScreensaver"
InstallDirRegKey HKLM "${PRODUCT_KEY}" "InstallDirectory"
ShowInstDetails show
ShowUninstDetails show

!define MUI_ABORTWARNING
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"

Function .onInit
  ${IfNot} ${RunningX64}
    MessageBox MB_ICONSTOP "${PRODUCT_NAME} requires 64-bit Windows."
    Abort
  ${EndIf}
FunctionEnd

Section "Install"
  SetOutPath "$INSTDIR"
  File /oname=BouncingScreensaver.scr "${SCR_PATH}"
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  CreateDirectory "$COMMONAPPDATA\BouncingScreensaver"

  WriteRegStr HKLM "${PRODUCT_KEY}" "InstallDirectory" "$INSTDIR"
  WriteRegStr HKLM "${PRODUCT_KEY}" "LogoSource" "$COMMONAPPDATA\BouncingScreensaver\logo.png"
  WriteRegDWORD HKLM "${PRODUCT_KEY}" "SpeedPxPerSecond" 220
  WriteRegDWORD HKLM "${PRODUCT_KEY}" "LogoWidthPercent" 15
  WriteRegStr HKLM "${PRODUCT_KEY}" "BackgroundColor" "#000000"
  WriteRegStr HKLM "${PRODUCT_KEY}" "FlashColor" "#7B2CFF"
  WriteRegDWORD HKLM "${PRODUCT_KEY}" "FlashEnabled" 1
  WriteRegDWORD HKLM "${PRODUCT_KEY}" "FlashDurationMs" 350
  WriteRegDWORD HKLM "${PRODUCT_KEY}" "CornerTolerancePx" 12

  WriteRegStr HKLM "${UNINSTALL_KEY}" "DisplayName" "${PRODUCT_NAME}"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "DisplayVersion" "${VERSION}"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "DisplayIcon" "$INSTDIR\BouncingScreensaver.scr"
  WriteRegStr HKLM "${UNINSTALL_KEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegStr HKLM "${UNINSTALL_KEY}" "QuietUninstallString" '"$INSTDIR\Uninstall.exe" /S'
  WriteRegDWORD HKLM "${UNINSTALL_KEY}" "NoModify" 1
  WriteRegDWORD HKLM "${UNINSTALL_KEY}" "NoRepair" 1
SectionEnd

Section "Uninstall"
  Delete "$INSTDIR\BouncingScreensaver.scr"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"

  DeleteRegKey HKLM "${UNINSTALL_KEY}"
  DeleteRegKey HKLM "${PRODUCT_KEY}"

  ; Intentionally preserve $COMMONAPPDATA\BouncingScreensaver so an externally
  ; managed logo is not deleted during upgrades/uninstall.
SectionEnd
