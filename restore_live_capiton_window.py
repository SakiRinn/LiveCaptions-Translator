import ctypes
import time
import psutil
import win32gui
import win32process
import subprocess
import os

try:
    import pywinauto
except ImportError:
    subprocess.run(["pip", "install", "pywinauto"])
    import pywinauto

GWL_EXSTYLE = -20
WS_EX_TOOLWINDOW = 0x00000080
SW_RESTORE = 9
SW_SHOW = 5
SW_MINIMIZE = 6

GetWindowLong = ctypes.windll.user32.GetWindowLongW
SetWindowLong = ctypes.windll.user32.SetWindowLongW
ShowWindow = ctypes.windll.user32.ShowWindow
SetForegroundWindow = ctypes.windll.user32.SetForegroundWindow

def find_live_captions_window():

    print("[*] Looking for LiveCaptions process...")
    for process in psutil.process_iter(attrs=['pid', 'name']):
        if process.info['name'].lower() == "livecaptions.exe":
            pid = process.info['pid']
            print(f"[+] LiveCaptions process found (PID: {pid})")

            def callback(hWnd, extra):
                _, found_pid = win32process.GetWindowThreadProcessId(hWnd)
                if found_pid == pid:
                    extra.append(hWnd)
                return True

            windows = []
            win32gui.EnumWindows(callback, windows)

            if windows:
                print(f"[+] LiveCaptions process found: {windows[0]}")
                return windows[0]

    print("[-] LiveCaptions window not found. Make sure you have manually started it.")
    return None

def restore_window(hWnd):
    if not hWnd:
        print("[-] Cannot found hWnd. Try other methods.")
        return False

    ex_style = GetWindowLong(hWnd, GWL_EXSTYLE)

    SetWindowLong(hWnd, GWL_EXSTYLE, ex_style & ~WS_EX_TOOLWINDOW)

    ShowWindow(hWnd, SW_RESTORE)
    SetForegroundWindow(hWnd)

    print("[+] LiveCaptions windows have been successfully restored")
    return True

def restart_live_captions():
    print("[!] Try to restart LiveCaptions process...")
    for process in psutil.process_iter(attrs=['pid', 'name']):
        if process.info['name'].lower() == "livecaptions.exe":
            print(f"[+] process (PID: {process.info['pid']})")
            process.kill()

    time.sleep(2)

    try:
        subprocess.Popen("LiveCaptions.exe", shell=True)
        print("[+] LiveCaptions windows have been successfully restarted")
    except Exception as e:
        print(f"[-] Failed to restart LiveCaptions: {e}")

def find_window_with_pywinauto():
    print("[*] Using pywinauto fingding the window...")
    try:
        app = pywinauto.Application().connect(path="LiveCaptions.exe", timeout=5)
        window = app.top_window()
        print(f"[+] Window found: {window.window_text()}")
        window.restore()
        window.set_focus()
        return True
    except Exception as e:
        print(f"[-] pywinauto failed to locate window: {e}")
        return False

if __name__ == "__main__":
    print("[*] Try to restore LiveCaptions window...")

    time.sleep(1) 
    hwnd = find_live_captions_window()

    if hwnd:
        success = restore_window(hwnd)
    else:
        success = find_window_with_pywinauto()

    if not success:
        print("[!] Cannot restore the window, try to restart the process...")
        restart_live_captions()

    print("[*] Finished！")
