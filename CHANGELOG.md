# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Support for new Brazilian alphanumeric CNPJ format (14 alphanumeric characters)
- `CnpjMaskFacade` class for unified CNPJ masking API
- Automatic detection of numeric vs alphanumeric CNPJ format
- Lowercase letters are automatically normalized to uppercase

### Changed
- CI/CD workflow now uses GitHub tags for versioning
- NuGet package version is extracted from git tag (e.g., `v1.5.0` → `1.5.0`)
- Package is only published to NuGet.org when a release is created
- Added .NET 8 and .NET 9 SDK support in CI pipeline

### Improved
- **Performance**: Single-pass extraction, validation, and normalization (was 3 passes)
- **Performance**: Removed interface dispatch overhead in hot path
- **Performance**: Fast ASCII character detection using bit manipulation
- **Performance**: Inline uppercase conversion using bit flip (`c & ~0x20`)
- **Performance**: Added `[MethodImpl(AggressiveInlining)]` to hot path methods
- Zero heap allocations except for final string result

### Fixed
- CS8600 warning in `MaskSensitiveDataAttribute.cs` (null reference)
- xUnit1012 warnings for nullable test parameters

### Removed
- Strategy pattern classes (`ICnpjMasker`, `NumericCnpjMasker`, `AlphaNumericCnpjMasker`) - functionality merged into optimized `CnpjMaskFacade`

## [1.4.3] - Previous Release

### Features
- CPF masking with customizable mask character
- CNPJ masking (numeric only)
- Email masking
- Credit card masking (Visa, MasterCard, Amex, Diners Club)
- Mobile phone masking (Brazilian format)
- Residential phone masking (Brazilian format)
- Vehicle license plate masking (Mercosul format)
- RG (Brazilian ID) masking
- Generic string masking with start position and length
- `[MaskSensitiveData]` attribute for automatic masking in models

---

## Version Format

This project uses git tags for versioning:
- Create a tag: `git tag v1.5.0`
- Push tag: `git push origin v1.5.0`
- Or create a GitHub Release, which automatically triggers NuGet publishing
