# MayorElection development rules

- Keep Domain independent of Data, Services and WinForms. The required IdentityUser dependency is intentional.
- Put business rules in Services, EF queries/configuration in Data, and presentation/binding in WinForms.
- Inject dependencies through constructors and register them in the composition root.
- Use Identity PasswordHasher/UserManager/authenticator providers. Never implement password hashing or production TOTP manually.
- Never log or commit passwords, authenticator keys, QR URIs, OTPs, runtime databases, or user exports.
- Keep SQLite connection strings in configuration. Use isolated temporary databases for tests.
- Preserve atomic writes and unique, contiguous candidate positions. Do not bypass SQLite constraints to reorder records.
- Use feature branches from develop and merge with --no-ff. Release branches start from develop; hotfixes start from main.
- Use English Conventional Commit messages. Do not rewrite published history or force-push without explicit user authorization.
- Before release, run restore/audit, Release build, tests and the pending-model-changes check. Keep README/changelog accurate about checks actually performed.
- Ask for missing remote/authentication information without requesting secrets in chat. Never commit build artifacts.
