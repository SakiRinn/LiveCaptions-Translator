import ctypes
import sys
from pathlib import Path

def get_resource_dir():
    """源码运行时返回脚本目录；PyInstaller 单文件运行时返回解压目录。"""
    if getattr(sys, 'frozen', False):
        return Path(getattr(sys, '_MEIPASS', Path(sys.executable).parent))
    return Path(__file__).parent

def get_db_path_from_args(argv):
    """支持 --db-path <path>，也兼容直接把路径作为第一个参数。"""
    if "--db-path" in argv:
        index = argv.index("--db-path")
        if index + 1 < len(argv):
            return argv[index + 1]
    if len(argv) > 1 and not argv[1].startswith("--"):
        return argv[1]
    return None

def load_openvr_dll():
    dll_path = get_resource_dir() / "libopenvr_api_64.dll"  # 匹配重命名后的文件
    if not dll_path.exists():
        raise FileNotFoundError(f"未找到DLL：{dll_path}")
    ctypes.CDLL(str(dll_path))

    # openvr 包通过 pkg_resources 查找无扩展名 DLL；在冻结环境中重定向到已打包的 DLL。
    import pkg_resources
    original_resource_filename = pkg_resources.resource_filename

    def patched_resource_filename(package_or_requirement, resource_name):
        if package_or_requirement == "openvr" and resource_name == "libopenvr_api_64":
            return str(dll_path)
        return original_resource_filename(package_or_requirement, resource_name)

    pkg_resources.resource_filename = patched_resource_filename

load_openvr_dll()
import openvr  # 替换为你的库，如 openvr、pygame 等
print(openvr.__file__)
import time
import sqlite3
import threading
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import sys


class SteamVRTranslationOverlay:
    def __init__(self, db_path=None):
        # 路径适配
        self.script_dir = get_resource_dir()
        if db_path:
            self.db_path = Path(db_path).expanduser().resolve()
        else:
            self.db_path = self.script_dir / "translation_history.db"
        self.font_path = self.script_dir / "font" / "gnuunifontfull-pm9p.ttf"
        self.temp_texture = self.script_dir / "temp_texture.png"

        # 头显固定位置参数
        self.hmd_position = {
            "x": 0.0,
            "y": 0.18,
            "z": -0.45,
            "width": 0.35
        }

        # 文本配置
        self.max_chars_per_line = 25  # 每行最大字数
        self.current_text = "等待翻译数据..."
        self.last_rowid = 0
        self.db_updated = False  # 数据库更新标志（关键）

        # SteamVR核心对象
        self.overlay_handle = None
        self.vr_overlay = None
        self.vr_system = None
        self.hmd_index = None

        # 状态控制
        self.running = True

        # 数据库配置
        self.table_name = "TranslationHistory"
        self.target_field = "TranslatedText"
        self.actual_field = None

        # 初始化检查
        self._check_resources()

    def _check_resources(self):
        if not self.font_path.exists():
            raise FileNotFoundError(f"字体缺失：{self.font_path}")
        if not self.db_path.exists():
            print(f"警告：数据库未找到({self.db_path})")

    def _get_hmd_index(self):
        for i in range(openvr.k_unMaxTrackedDeviceCount):
            if self.vr_system.isTrackedDeviceConnected(i):
                if self.vr_system.getTrackedDeviceClass(i) == openvr.TrackedDeviceClass_HMD:
                    return i
        return openvr.k_unTrackedDeviceIndexInvalid

    def _init_steamvr(self):
        try:
            openvr.init(openvr.VRApplication_Overlay)
            self.vr_system = openvr.VRSystem()
            self.vr_overlay = openvr.IVROverlay()

            overlay_key = "translation_overlay.db_triggered"
            overlay_name = "翻译显示（数据库触发更新）"
            self.overlay_handle = self.vr_overlay.createOverlay(overlay_key, overlay_name)

            self.hmd_index = self._get_hmd_index()
            if self.hmd_index == openvr.k_unTrackedDeviceIndexInvalid:
                print("错误：未检测到头显")
                openvr.shutdown()
                return False

            # 设置头显相对位置
            transform = openvr.HmdMatrix34_t()
            transform[0][0] = transform[1][1] = transform[2][2] = 1.0
            transform[0][3] = self.hmd_position["x"]
            transform[1][3] = self.hmd_position["y"]
            transform[2][3] = self.hmd_position["z"]
            self.vr_overlay.setOverlayWidthInMeters(self.overlay_handle, self.hmd_position["width"])

            self.vr_overlay.setOverlayTransformTrackedDeviceRelative(
                self.overlay_handle, self.hmd_index, transform
            )

            self.vr_overlay.showOverlay(self.overlay_handle)
            print("字幕已固定在头显水平正前方（数据库变化时更新）")
            return True

        except openvr.OpenVRError as e:
            print(f"SteamVR初始化失败：{e}")
            openvr.shutdown()
            return False

    def _wrap_text(self, text):
        """文本自动换行处理"""
        wrapped_lines = []
        for i in range(0, len(text), self.max_chars_per_line):
            wrapped_lines.append(text[i:i + self.max_chars_per_line])
        return '\n'.join(wrapped_lines)

    def _render_text(self, text, font_size=36):
        """渲染文本为图像"""
        try:
            font = ImageFont.truetype(str(self.font_path), font_size)
        except IOError as e:
            raise RuntimeError(f"字体加载失败：{e}")

        wrapped_text = self._wrap_text(text)
        temp_img = Image.new("RGBA", (1, 1), (0, 0, 0, 0))
        draw = ImageDraw.Draw(temp_img)
        bbox = draw.textbbox((0, 0), wrapped_text, font=font)
        text_width = bbox[2] - bbox[0]
        text_height = bbox[3] - bbox[1]

        padding = 15
        img = Image.new(
            "RGBA",
            (text_width + 2 * padding, text_height + 2 * padding),
            (0, 0, 0, 200)
        )
        draw = ImageDraw.Draw(img)
        draw.text(
            (padding - bbox[0], padding - bbox[1]),
            wrapped_text,
            font=font,
            fill=(255, 255, 255, 255)
        )
        return img

    def _update_overlay(self):
        """仅当数据库更新时才刷新VR显示（核心优化）"""
        if not self.overlay_handle:
            return

        # 检查是否有数据库更新，有则刷新显示
        if self.db_updated:
            try:
                text_img = self._render_text(self.current_text)
                text_img.save(self.temp_texture)
                self.vr_overlay.setOverlayFromFile(self.overlay_handle, str(self.temp_texture))
                print(f"VR显示已更新：{self.current_text[:30]}...")  # 打印前30字
                self.db_updated = False  # 重置更新标志
            except Exception as e:
                print(f"Overlay更新失败：{e}")

    def _validate_database(self, cursor):
        try:
            cursor.execute(f"PRAGMA table_info({self.table_name})")
            columns = [col[1] for col in cursor.fetchall()]
            if self.target_field not in columns:
                print(f"错误：表 {self.table_name} 缺少字段 {self.target_field}")
                print(f"当前字段：{columns}")
                return False
            self.actual_field = self.target_field
            return True
        except Exception as e:
            print(f"数据库验证失败：{e}")
            return False

    def _load_latest(self):
        """初始加载最新记录"""
        if not self.db_path.exists():
            return
        try:
            conn = sqlite3.connect(str(self.db_path))
            cursor = conn.cursor()

            if not self._validate_database(cursor):
                conn.close()
                return

            cursor.execute(f"""
                SELECT Id, {self.actual_field}
                FROM {self.table_name}
                ORDER BY Id DESC
                LIMIT 1
            """)
            latest = cursor.fetchone()
            if latest:
                self.last_rowid, self.current_text = latest
                self.db_updated = True  # 触发初始显示更新
                print(f"初始加载最新翻译：{self.current_text}")
            conn.close()
        except Exception as e:
            print(f"加载数据失败：{e}")

    def _monitor_db(self):
        """监听数据库变化，仅在有新记录时设置更新标志"""
        while self.running:
            if not self.db_path.exists():
                time.sleep(2)
                continue
            try:
                conn = sqlite3.connect(str(self.db_path))
                cursor = conn.cursor()

                if not self.actual_field and not self._validate_database(cursor):
                    conn.close()
                    time.sleep(5)
                    continue

                # 只查询ID大于最后一次记录的新数据
                cursor.execute(f"""
                    SELECT Id, {self.actual_field}
                    FROM {self.table_name}
                    WHERE Id > ?
                    ORDER BY Id DESC
                    LIMIT 1
                """, (self.last_rowid,))
                new_row = cursor.fetchone()

                if new_row:
                    self.last_rowid, self.current_text = new_row
                    self.db_updated = True  # 设置更新标志（关键）
                    print(f"检测到新翻译（ID: {self.last_rowid}）：{self.current_text}")

                conn.close()
            except Exception as e:
                print(f"数据库监听错误：{e}")
                time.sleep(3)
            time.sleep(1)  # 每秒检查一次

    def start(self):
        if not self._init_steamvr():
            return

        self._load_latest()
        self._update_overlay()  # 显示初始内容

        # 启动数据库监听线程
        db_thread = threading.Thread(target=self._monitor_db, daemon=True)
        db_thread.start()

        # 主循环：仅在有更新时才实际刷新
        try:
            print("Overlay启动（数据库变化时自动更新），按Ctrl+C退出...")
            while self.running:
                self._update_overlay()  # 这里会检查db_updated标志
                time.sleep(0.1)  # 高频检查但不频繁渲染
        except KeyboardInterrupt:
            print("退出中...")
        finally:
            self.running = False
            db_thread.join(timeout=2)
            self._shutdown()

    def _shutdown(self):
        if self.vr_overlay and self.overlay_handle:
            self.vr_overlay.hideOverlay(self.overlay_handle)
            self.vr_overlay.destroyOverlay(self.overlay_handle)
        openvr.shutdown()
        print("程序退出")


if __name__ == "__main__":
    try:
        db_path = get_db_path_from_args(sys.argv)
        app = SteamVRTranslationOverlay(db_path=db_path)
        print(f"使用数据库：{app.db_path}")
        app.start()
    except Exception as e:
        print(f"启动失败：{e}", flush=True)
        if not getattr(sys, 'frozen', False) and sys.stdin.isatty():
            input("按任意键关闭...")
        sys.exit(1)
