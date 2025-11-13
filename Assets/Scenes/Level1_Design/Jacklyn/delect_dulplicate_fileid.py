import re
import os
import sys

def scan_unity_file(path):
    with open(path, "r", encoding="utf-8") as f:
        lines = f.readlines()

    pattern = re.compile(r"^--- !u!\d+ &(\d+)")
    ids = {}
    duplicates = {}

    for i, line in enumerate(lines):
        match = pattern.match(line)
        if match:
            file_id = match.group(1)
            if file_id in ids:
                duplicates.setdefault(file_id, []).append(i + 1)
            else:
                ids[file_id] = i + 1

    print(f"\n📄 文件: {path}")
    if not duplicates:
        print("✅ 没有检测到重复的 fileID。")
        return

    print(f"⚠️ 检测到 {len(duplicates)} 个重复 fileID：\n")
    for fid, locs in duplicates.items():
        print(f"  fileID {fid} 出现在行号: {ids[fid]}, {', '.join(map(str, locs))}")
    print()

    return duplicates


def auto_fix_duplicates(path, duplicates):
    backup_path = path + ".bak"
    os.rename(path, backup_path)
    print(f"📦 已备份原文件到 {backup_path}")

    with open(backup_path, "r", encoding="utf-8") as f:
        lines = f.readlines()

    pattern = re.compile(r"^--- !u!\d+ &(\d+)")
    seen = set()
    new_lines = []
    skip_mode = False
    current_id = None

    for line in lines:
        match = pattern.match(line)
        if match:
            fid = match.group(1)
            if fid in seen:
                skip_mode = True
                current_id = fid
                print(f"🗑️ 删除重复块 fileID {fid}")
                continue
            else:
                seen.add(fid)
                skip_mode = False
        if not skip_mode:
            new_lines.append(line)

    new_path = path
    with open(new_path, "w", encoding="utf-8") as f:
        f.writelines(new_lines)

    print(f"✅ 修复完成，文件已写回到 {new_path}")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("用法: python detect_duplicate_fileid.py <path_to_unity_scene>")
        sys.exit(1)

    path = sys.argv[1]
    duplicates = scan_unity_file(path)
    if duplicates:
        choice = input("\n是否自动修复重复 fileID？(y/n): ").strip().lower()
        if choice == "y":
            auto_fix_duplicates(path, duplicates)