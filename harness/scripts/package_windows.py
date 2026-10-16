"""Package an existing Windows publish with SDK-free test launchers (stdlib only)."""

import argparse
import hashlib
import json
from pathlib import Path
import struct
import zipfile


def package_windows(published: Path, destination: Path) -> dict:
    published = published.resolve()
    destination = destination.resolve()
    for name in (
        "VoxPet.exe",
        "USER-GUIDE.txt",
        "Assets/Characters/manifest.json",
        "Assets/ImageSetTemplate/imageset.json",
        "Assets/Samples/studio-samples.voxpet",
        "Assets/Samples/companion-neulbo.voxpet",
        "Assets/Samples/companion-pelly.voxpet",
        "Assets/Samples/LICENSE.txt",
        "Licenses/NAudio-LICENSE.txt",
        "Licenses/DOTNET-LICENSE.txt",
    ):
        if not (published / name).is_file():
            raise ValueError(f"Missing publish file: {name}")
    with (published / "VoxPet.exe").open("rb") as exe:
        header = exe.read(64)
        if len(header) != 64 or header[:2] != b"MZ":
            raise ValueError("Expected a Windows PE executable")
        exe.seek(struct.unpack_from("<I", header, 60)[0])
        if exe.read(6) != b"PE\0\0\x64\x86":
            raise ValueError("Expected an x64 PE executable")
    launchers = Path(__file__).resolve().parents[1] / "windows-test"
    files = [(p, p.relative_to(published).as_posix()) for p in sorted(published.rglob("*")) if p.is_file() and p.suffix.lower() != ".pdb"]
    files += [(launchers / name, name) for name in ("START.cmd", "TEST-SMOKE.cmd", "TEST-SMOKE.ps1", "TEST-README.txt")]
    names = [name.casefold() for _, name in files]
    if len(names) != len(set(names)) or any(p.is_symlink() for p, _ in files):
        raise ValueError("Duplicate names or symbolic links in publish")
    if destination.is_relative_to(published):
        raise ValueError("Write the ZIP outside the publish directory")
    destination.parent.mkdir(parents=True, exist_ok=True)
    # Existing packages are preserved; callers choose a new output name.
    with destination.open("xb") as output:
        try:
            with zipfile.ZipFile(output, "w", compression=zipfile.ZIP_DEFLATED) as archive:
                for path, name in files:
                    archive.write(path, name)
        except BaseException:
            output.close()
            destination.unlink()
            raise
    with zipfile.ZipFile(destination) as archive:
        bad = archive.testzip()
        if bad:
            raise ValueError(f"ZIP CRC failure: {bad}")
    digest = hashlib.sha256()
    with destination.open("rb") as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(chunk)
    return {"path": str(destination), "files": len(files), "bytes": destination.stat().st_size, "sha256": digest.hexdigest(), "crc": "passed"}


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("published", type=Path)
    parser.add_argument("destination", type=Path)
    args = parser.parse_args()
    print(json.dumps(package_windows(args.published, args.destination), ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
