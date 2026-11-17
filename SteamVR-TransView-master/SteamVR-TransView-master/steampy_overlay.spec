# -*- mode: python ; coding: utf-8 -*-


a = Analysis(
    ['steampy_overlay.py'],
    pathex=[],
    binaries=[],
    datas=[
        ('libopenvr_api_64.dll', '.'),
        ('font/gnuunifontfull-pm9p.ttf', 'font'),
    ],
    hiddenimports=['pkg_resources'],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    noarchive=False,
)
pyz = PYZ(a.pure)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.datas,
    [],
    name='SteamVR-TransView',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    console=True,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
    onefile=True,
)
