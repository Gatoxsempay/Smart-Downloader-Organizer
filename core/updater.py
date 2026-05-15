import threading
from typing import Callable, Optional

try:
    import requests
    from packaging import version as pkg_version
    _DEPS_OK = True
except ImportError:
    _DEPS_OK = False


def check_for_updates(
    current_version: str,
    check_url: str,
    callback: Callable[[Optional[dict]], None],
):
    def _run():
        if not _DEPS_OK or not check_url:
            callback(None)
            return
        try:
            resp = requests.get(
                check_url, timeout=6,
                headers={"Accept": "application/vnd.github.v3+json"},
            )
            if resp.status_code != 200:
                callback(None)
                return
            data = resp.json()
            tag = data.get("tag_name", "").lstrip("v")
            if not tag:
                callback(None)
                return
            if pkg_version.parse(tag) > pkg_version.parse(current_version):
                callback({
                    "version": tag,
                    "url": data.get("html_url", ""),
                    "notes": data.get("body", ""),
                    "assets": [a["browser_download_url"] for a in data.get("assets", [])],
                })
            else:
                callback({"version": None})
        except Exception:
            callback(None)

    threading.Thread(target=_run, daemon=True).start()
