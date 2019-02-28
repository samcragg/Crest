# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.1] - 2019-02-28

### Added

- Classes for returning links to other endpoints in the application (see
  `LinkBuilder` and `ILinkProvider`)

## [0.2.0] - 2019-02-14

### Added

- Support for custom serialization of types via the new `ICustomSerializer<T>`
  interface

### Changed

- The URL templates use a format that is compatible with RFC 6570

## [0.1.3] - 2018-10-29

### Added

- Dynamic query key/value capturing
- `DataAccess` library, provides helpers to filter and sort via the query string

### Changed

- More use of `Span`s internally

## [0.1.2] - 2018-08-28

### Fixed

- Minor build infrastructure improvements

## [0.1.1] - 2018-08-26

### Added

- Initial developer release for using with the example project
