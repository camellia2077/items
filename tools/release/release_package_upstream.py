from __future__ import annotations

import hashlib
import json
import os
import shutil
import urllib.request
import zipfile
from pathlib import Path


HTTP_USER_AGENT = "RandomLoadoutReleasePackager/0.2.3"


def sha256_for_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as file_obj:
        for chunk in iter(lambda: file_obj.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def load_metadata(metadata_path: Path) -> dict:
    if not metadata_path.is_file():
        raise FileNotFoundError("Release package metadata not found: {0}".format(metadata_path))

    with metadata_path.open("r", encoding="utf-8") as file_obj:
        return json.load(file_obj)


def download_to_path(url: str, destination_path: Path) -> None:
    destination_path.parent.mkdir(parents=True, exist_ok=True)
    request = urllib.request.Request(url, headers={"User-Agent": HTTP_USER_AGENT})
    with urllib.request.urlopen(request) as response, destination_path.open("wb") as output:
        shutil.copyfileobj(response, output)


def download_text(url: str) -> str:
    request = urllib.request.Request(url, headers={"User-Agent": HTTP_USER_AGENT})
    with urllib.request.urlopen(request) as response:
        return response.read().decode("utf-8")


def ensure_cached_upstream_archive(cache_directory: Path, metadata: dict) -> Path:
    upstream_package = metadata["upstreamPackage"]
    download_url = upstream_package["downloadUrl"]
    expected_hash = upstream_package["sha256"]
    archive_name = Path(download_url).name
    archive_path = cache_directory / archive_name

    if archive_path.is_file() and sha256_for_file(archive_path) == expected_hash:
        return archive_path

    if archive_path.exists():
        archive_path.unlink()

    download_to_path(download_url, archive_path)
    actual_hash = sha256_for_file(archive_path)
    if actual_hash != expected_hash:
        if archive_path.exists():
            archive_path.unlink()
        raise OSError(
            "Upstream archive hash mismatch for '{0}'. Expected {1}, got {2}.".format(
                archive_name, expected_hash, actual_hash
            )
        )

    return archive_path


def extract_upstream_content(archive_path: Path, metadata: dict, staging_root: Path) -> None:
    content_root = metadata["upstreamPackage"]["contentRoot"]
    extracted_file_count = 0
    with zipfile.ZipFile(str(archive_path), "r") as zip_file:
        for member in zip_file.infolist():
            if member.is_dir() or not member.filename.startswith(content_root):
                continue

            relative_name = member.filename[len(content_root) :]
            if not relative_name:
                continue

            target_path = staging_root / relative_name
            target_path.parent.mkdir(parents=True, exist_ok=True)
            with zip_file.open(member, "r") as source_file, target_path.open("wb") as output_file:
                shutil.copyfileobj(source_file, output_file)

            permissions = member.external_attr >> 16
            if permissions:
                os.chmod(str(target_path), permissions)

            extracted_file_count += 1

    if extracted_file_count == 0:
        raise OSError(
            "No upstream package files were extracted from '{0}' using content root '{1}'.".format(
                archive_path, content_root
            )
        )
