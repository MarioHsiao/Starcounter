## [Unreleased][unreleased]

### Changed
- Removal of notion of Polyjuice and major refactoring around this. Full list of changes is here:
[Starcounter.io blog](http://starcounter.io/nightly-changes/list-breaking-changes-starting-build-2-0-3500-3/)
- Applications are now isolated on SQL level, read more here:
[SQL isolation](http://starcounter.io/guides/sql/sql-isolation/)
- Static files from /sys/ folder migrated to Polymer 1.1: [Roadmap to Polymer 1.1](https://github.com/Starcounter/Starcounter/issues/2854)

### Fixed
- Unandled exceptions in UDP/TCP handlers:
https://github.com/Starcounter/Starcounter/issues/2886
- Setting AppName in DbSession.* calls, as well as processing unhandled exceptions there.

