# Licensing Notes (Not Legal Advice)

You asked to sell the app without licensing surprises.

This starter uses LibVLC / LibVLCSharp, which is LGPL-licensed. That can be compatible with selling software, but you must comply with the license obligations.

## What we do in this repo to help

- Include the LGPL-2.1 license text: `licenses/LGPL-2.1.txt`
- Provide a `THIRD_PARTY_NOTICES.md` file listing major dependencies.
- Keep LibVLC in separate native binaries (via the VideoLAN.LibVLC.Windows NuGet), rather than copying VLC source into your code.

## What you should do before selling

- Have a lawyer review your distribution and compliance plan.
- Confirm:
  - how you will provide license notices to users
  - how you will provide source / relinking mechanism if required
  - whether your use of VLC plugins/codecs triggers additional obligations

## Why this matters

Installer packaging can accidentally hide license obligations. We keep all notices in the repo and recommend shipping them inside the installer as well.

## Disclaimer

This is an engineering checklist only and is not legal advice.
