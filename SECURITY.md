# Security Policy

Thank you for taking the time to report a security issue.

The security of RoboSharp and the applications that depend on it is important. If you believe you have discovered a security vulnerability, please report it responsibly.

## Supported Versions

Security fixes are only provided for the latest stable release.

| Version | Supported |
|----------|-----------|
| Latest release | ✅ |
| Older releases | ❌ |
| Development builds | ❌ |

Please ensure you are using the latest version before reporting a vulnerability.

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub Issues.**

Instead, use one of the following methods:

- Open a **GitHub Private Vulnerability Report** (preferred)
- Contact the project maintainer directly via GitHub

Please include:

- RoboSharp version
- .NET version
- Windows version
- A description of the vulnerability
- Steps to reproduce
- Proof-of-concept code (if available)
- Any assessment of the potential impact

## What Happens Next?

After receiving your report I will:

- Acknowledge receipt as soon as practical.
- Investigate the issue privately.
- Work on a fix if the issue is confirmed.
- Credit the reporter (if desired) when the fix is publicly released.

Please allow reasonable time for investigation before publicly disclosing the issue.

## Scope

Examples of security issues include, but are not limited to:

- Arbitrary file access or deletion
- Path traversal vulnerabilities
- Symlink or junction attacks
- Privilege escalation
- Remote code execution
- Denial of service caused by crafted input

The following are generally **not** considered security vulnerabilities:

- General bugs
- Performance issues
- Feature requests
- Robocopy behaviour that matches Microsoft's implementation
- Issues that require administrator access without introducing additional security impact

## Third-Party Components

RoboSharp is a managed .NET wrapper around the Windows **Robocopy** utility.

Security issues affecting Robocopy itself should be reported to Microsoft, as they are outside the scope of this project.

## Security Best Practices

When using RoboSharp:

- Validate user-supplied paths.
- Avoid running applications with unnecessary administrative privileges.
- Ensure copy destinations are trusted.
- Keep Windows, .NET and RoboSharp up to date.

Thank you for helping make RoboSharp more secure.
