# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| 0.1.x   | Yes       |

## Reporting a Vulnerability

If you discover a security vulnerability in this package, please report it responsibly:

1. **Do not** open a public GitHub issue
2. Email: **security@bizsim.com**
3. Include: package name, version, description of the vulnerability, and steps to reproduce

We will acknowledge your report within 48 hours and provide a fix timeline within 7 days.

## Scope

This is an Editor-only package with no runtime code. It does not handle user data,
network requests, or sensitive information. Security concerns are limited to:

- Scripting define symbol manipulation (build-time only)
- Assembly scanning (read-only, in-process)
