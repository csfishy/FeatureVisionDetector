# Security Policy

## Supported versions

FeatureVisionDetector is currently an alpha project. Security fixes are applied to the latest code on `main` and, when a prerelease exists, to the newest prerelease only.

| Version | Supported |
| --- | --- |
| `main` | Yes |
| Latest `0.x` prerelease | Yes |
| Older snapshots | No |

## Reporting a vulnerability

Please use GitHub's **Security > Report a vulnerability** flow so details remain private. Include the affected revision, operating system, a minimal proof of concept or malformed package when safe to share, observed impact, and any suggested mitigation.

Do not open a public issue for an unpatched vulnerability. If private vulnerability reporting is not available, contact the maintainer through the GitHub profile linked to this repository and request a private reporting channel without including exploit details in the initial message.

You should receive an acknowledgement within seven days. The maintainer will validate the report, assess affected versions, coordinate a fix and disclosure timeline, and credit the reporter unless anonymity is requested.

## Security boundaries

The most important untrusted inputs are `.fvfeature` ZIP archives, their JSON manifests and numeric settings, PNG/JPEG image data, and future third-party pull requests. Image data is eventually processed by System.Drawing and native OpenCV code. Reports involving path traversal, archive expansion, parser crashes, memory or CPU exhaustion, unsafe native dependencies, camera lifecycle issues, or secret exposure are in scope.
