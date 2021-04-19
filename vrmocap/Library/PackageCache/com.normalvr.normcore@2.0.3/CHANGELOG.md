# Normcore Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.3] - 2020-11-13

### Fixed
- Fix exception on `RealtimeSet#modelRemoved` when event is null.
- Fix issue introduced in 2.0.2 with `RealtimeTransform` on scene views not initializing correctly.

## [2.0.2] - 2020-11-9

### Added
- Add `preventOwnershipTakeover` and `destroyWhenOwnerOrLastClientLeaves` properties to `RealtimeView`.
- Add support for macOS 10.13 & 10.14 (High Sierra & Mojave).
- Notarize native plugin on macOS.

### Fixed
- Add Lumin SDK to list of supported platforms for native plugin to fix Magic Leap support.
- Fix null `realtimeView` references on disabled game objects.

## [2.0.1] - 2020-10-26

### Added
- Add "Never Ask Again" option when Normcore alerts you to a new version.

### Fixed
- Fix thread error when a `RealtimeTransform` is garbage collected off the main thread.

## [2.0.0] - 2020-10-16
Initial changelog release.
