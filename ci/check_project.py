"""Kiểm tra nhanh cấu trúc dự án Unity, không cần mở Unity.

Chạy: python ci/check_project.py [thư mục dự án]   (mặc định: thư mục cha của ci/)

Kiểm tra:
- Mỗi file/thư mục trong Assets/ có file .meta, và không có .meta mồ côi.
- Không có GUID trùng giữa các file .meta.
- Các file JSON (manifest, input actions) hợp lệ.
- Scene trong Build Settings tồn tại và đúng GUID.
- Tham chiếu script trong scene/prefab trỏ tới script có thật (bỏ qua script của Unity/package).
- Không có file vượt giới hạn GitHub (100 MB), cảnh báo file > 50 MB.
"""
import json
import os
import re
import sys

ROOT = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(__file__), ".."))
ASSETS = os.path.join(ROOT, "Assets")
WARN_MB, FAIL_MB = 50, 100

errors: list[str] = []
warnings: list[str] = []


def rel(path: str) -> str:
    return os.path.relpath(path, ROOT).replace("\\", "/")


def read_guid(meta_path: str) -> str | None:
    with open(meta_path, encoding="utf-8", errors="replace") as f:
        for line in f:
            if line.startswith("guid:"):
                return line.split(":", 1)[1].strip()
    return None


def is_hidden(name: str) -> bool:
    # Unity bỏ qua file/thư mục bắt đầu bằng "." hoặc kết thúc bằng "~"
    return name.startswith(".") or name.endswith("~")


def check_meta_files() -> dict[str, str]:
    guids: dict[str, str] = {}
    for dirpath, dirnames, filenames in os.walk(ASSETS):
        dirnames[:] = [d for d in dirnames if not is_hidden(d)]
        names = set(filenames) | set(dirnames)
        for name in sorted(names):
            if is_hidden(name):
                continue
            path = os.path.join(dirpath, name)
            if name.endswith(".meta"):
                target = path[:-5]
                if not os.path.exists(target):
                    errors.append(f".meta mồ côi (không có asset): {rel(path)}")
                    continue
                guid = read_guid(path)
                if not guid:
                    errors.append(f".meta không có guid: {rel(path)}")
                elif guid in guids:
                    errors.append(f"GUID trùng {guid}: {rel(path)} và {guids[guid]}")
                else:
                    guids[guid] = rel(path)
            elif not os.path.exists(path + ".meta"):
                errors.append(f"Thiếu file .meta: {rel(path)}")
    return guids


def check_json() -> None:
    files = [os.path.join(ROOT, "Packages", "manifest.json")]
    for dirpath, dirnames, filenames in os.walk(ASSETS):
        files += [os.path.join(dirpath, f) for f in filenames if f.endswith((".inputactions", ".json"))]
    for path in files:
        try:
            with open(path, encoding="utf-8-sig") as f:
                json.load(f)
        except (OSError, ValueError) as e:
            errors.append(f"JSON lỗi: {rel(path)} — {e}")


def check_build_scenes() -> None:
    path = os.path.join(ROOT, "ProjectSettings", "EditorBuildSettings.asset")
    with open(path, encoding="utf-8") as f:
        text = f.read()
    scenes = re.findall(r"- enabled: (\d)\s+path: (.+?)\s+guid: ([0-9a-f]+)", text)
    if not any(enabled == "1" for enabled, _, _ in scenes):
        errors.append("Build Settings không có scene nào được bật")
    for enabled, scene, guid in scenes:
        scene_path = os.path.join(ROOT, scene)
        if not os.path.exists(scene_path):
            errors.append(f"Scene trong Build Settings không tồn tại: {scene}")
        elif read_guid(scene_path + ".meta") != guid:
            errors.append(f"GUID scene trong Build Settings không khớp file .meta: {scene}")


def check_script_references(guids: dict[str, str]) -> None:
    """Báo lỗi khi scene/prefab gắn component với guid trỏ tới một asset trong Assets/ không phải file .cs
    (thường do .meta bị tạo lại hoặc đổi tên sai). Guid không có trong Assets/ được bỏ qua vì có thể là
    script của Unity/package — script thiếu hẳn sẽ do job build Unity phát hiện."""
    script_guids = {g for g, p in guids.items() if p.endswith(".cs.meta")}
    pattern = re.compile(r"m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32}), type: 3\}")
    for dirpath, dirnames, filenames in os.walk(ASSETS):
        for name in filenames:
            if not name.endswith((".unity", ".prefab", ".asset")):
                continue
            path = os.path.join(dirpath, name)
            with open(path, encoding="utf-8", errors="replace") as f:
                text = f.read()
            for guid in set(pattern.findall(text)):
                if guid in guids and guid not in script_guids:
                    errors.append(f"{rel(path)} tham chiếu script guid {guid} nhưng đó không phải file .cs: {guids[guid]}")


def check_file_sizes() -> None:
    for top in ("Assets", "Packages", "ProjectSettings"):
        for dirpath, _, filenames in os.walk(os.path.join(ROOT, top)):
            for name in filenames:
                path = os.path.join(dirpath, name)
                mb = os.path.getsize(path) / (1024 * 1024)
                if mb > FAIL_MB:
                    errors.append(f"File {mb:.1f} MB vượt giới hạn GitHub {FAIL_MB} MB: {rel(path)}")
                elif mb > WARN_MB:
                    warnings.append(f"File lớn {mb:.1f} MB (nên dùng Git LFS): {rel(path)}")


def main() -> int:
    sys.stdout.reconfigure(encoding="utf-8")
    guids = check_meta_files()
    check_json()
    check_build_scenes()
    check_script_references(guids)
    check_file_sizes()

    for w in warnings:
        print(f"::warning::{w}" if os.environ.get("GITHUB_ACTIONS") else f"CẢNH BÁO: {w}")
    for e in errors:
        print(f"::error::{e}" if os.environ.get("GITHUB_ACTIONS") else f"LỖI: {e}")
    print(f"Đã kiểm tra {len(guids)} asset — {len(errors)} lỗi, {len(warnings)} cảnh báo.")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
