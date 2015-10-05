## [Unreleased][unreleased]

### Changed
- Removal of notion of Polyjuice and major refactoring around this. Full list of changes is here:
[Starcounter.io blog](http://starcounter.io/nightly-changes/list-breaking-changes-starting-build-2-0-3500-3/)
- Applications are now isolated on SQL level, read more here:
[SQL isolation](http://starcounter.io/guides/sql/sql-isolation/)
- Static files from /sys/ folder migrated to Polymer 1.1: [Roadmap to Polymer 1.1](https://github.com/Starcounter/Starcounter/issues/2854)
- UriMapping.OntologyMap now allows only use of fully namespaced class names. Recommended string prefix has changed from "/db/" and "/so/" to UriMapping.OntologyMappingUriPrefix (which is "/sc/db"), for example: UriMapping.OntologyMappingUriPrefix + "/simplified.ring6.chatattachment/@w". As a second parameter you can now simply supply just a fully namespaced class name, like this: "simplified.ring6.chatattachment".
- REST Call ```GET /api/admin/database/[Name]/applications``` Changed to ```GET /api/admin/databases/[Name]/applications``` and 
 ```GET /api/admin/database/[Name]/appstore/stores``` Changed to ```GET /api/admin/databases/[Name]/appstore/stores```. Notice the plural in ```databases```
 - Renamed the scnetworkgateway.xml in StarcounterBin to scnetworkgateway.sample.xml
 - Moved requirement to have at least 2 CPU cores to recommendations, as 4Gb RAM now.


### Fixed
- Unandled exceptions in UDP/TCP handlers:
https://github.com/Starcounter/Starcounter/issues/2886
- Setting AppName in DbSession.* calls, as well as processing unhandled exceptions there.