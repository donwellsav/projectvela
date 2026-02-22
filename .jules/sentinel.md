## 2025-05-15 - Unvalidated Configuration Inputs
**Vulnerability:** `AppSettings` loaded file paths and `AllowedExtensions` directly from JSON without validation. An attacker with write access to `settings.json` could enable execution of arbitrary files (by adding `.exe` to extensions) or manipulate file operations via path traversal.
**Learning:** Configuration files are untrusted input sources. Even if the app is offline/desktop, local privilege escalation or persistence via config modification is a risk.
**Prevention:** Always sanitize configuration values immediately after loading, especially file paths and allowlists that control execution or file access.
