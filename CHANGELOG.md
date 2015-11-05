## [Unreleased][unreleased]
### Changed
- New staradmin command `staradmin start server`, as requested in [#2950](https://github.com/Starcounter/Starcounter/issues/2950) and documented at [staradmin CLI](http://starcounter.io/guides/tools/staradmin/).
- New staradmin command `staradmin start database`, as requested in [#2950](https://github.com/Starcounter/Starcounter/issues/2950) and documented at [staradmin CLI](http://starcounter.io/guides/tools/staradmin/). By default, this command also automatically create the specified database if it does not exist.
- Support for new command `staradmin new db` allowing simple creation of databases from the command-line, as requested in [#2973](https://github.com/Starcounter/Starcounter/issues/2973) and documented at [staradmin CLI](http://starcounter.io/guides/tools/staradmin/).
- Added support for new command option --failmissing to `staradmin delete`, forcing the operation to fail if the specified artifact does not exist, as described in [#2995](https://github.com/Starcounter/Starcounter/issues/2995). Documented at [staradmin CLI](http://starcounter.io/guides/tools/staradmin/).
- Support for transient classes, [#3010](https://github.com/Starcounter/Starcounter/issues/3010).
- New feature: assembly level [Database] declaration, [#3005](https://github.com/Starcounter/Starcounter/issues/3005).
- Simplifications when passing source code to `star.exe`, [#3004](https://github.com/Starcounter/Starcounter/issues/3004) and [#3011](https://github.com/Starcounter/Starcounter/issues/3011).
- Introduced support for transacted entrypoints with `star --transact`, [#3008](https://github.com/Starcounter/Starcounter/issues/3008).
- Native code binaries are switched to use Visual Studio 2015 CRT. Installer now contains CRT setups that are installed in the system, if not present. The installer size increased because of that.

### Fixed
- Bug fixed for inheritance of objects and arrays in TypedJSON that caused null references: [#2955](https://github.com/Starcounter/Starcounter/issues/2955) 
- Fixed issue with setting outgoing fields and using outgoing filters in relation to static file resources responses: [#2961](https://github.com/Starcounter/Starcounter/issues/2961).
- Fixed issue with missing AppName and PartialId in serialized json when running Launcher: [#2902](https://github.com/Starcounter/Starcounter/issues/2902)
- Fixed an issue when Administrator was starting faster than gateway process in scservice: [#2962](https://github.com/Starcounter/Starcounter/issues/2962)
- Fixed text input and text selection issues in Administrator[#2942](https://github.com/Starcounter/Starcounter/issues/2942), [#2400](https://github.com/Starcounter/Starcounter/issues/2400), [#1993](https://github.com/Starcounter/Starcounter/issues/1993)
- Fixed max column width issue in Administrator[#2828](https://github.com/Starcounter/Starcounter/issues/2828)
- Fixed incorrect invalidation of databinding for bound properties in TypedJSON: [#2998](https://github.com/Starcounter/Starcounter/issues/2998)
- Fixed bug caused by using synonyms in new builds: [#2997](https://github.com/Starcounter/Starcounter/issues/2997)
- Removed (not implemented) option `staradmin delete log` as decided in [#2974](https://github.com/Starcounter/Starcounter/issues/2974).

## [2.1.163] - 2015-10-14
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
- `Response.HeadersDictionary` is gone and replaced with function `Response.SetHeadersDictionary`. To set individual headers use `response["HeaderName"] = "HeaderValue"` syntax.
- `HandlerOptions.ProxyDelegateTrigger` is now an internal flag that is no longer exposed to user applications (affected Launcher app).
- Starcounter is always installed in a subfolder called "Starcounter".
- Version number in the installation path was removed.

### Fixed
- Unhandled exceptions in UDP/TCP handlers:
https://github.com/Starcounter/Starcounter/issues/2886
- Setting AppName in DbSession.* calls, as well as processing unhandled exceptions there.
- Code rewritten for detecting changes on bound arrays in TypedJSON to avoid producing unnecessary changes: [#2920](https://github.com/Starcounter/Starcounter/issues/2920).
- Bug fixed concerning indexes on database objects. Combined indexes might need to be recreated to work properly: [#2933](https://github.com/Starcounter/Starcounter/issues/2933). 
- Bug fixed regarding headers dictionary creation (CreateHeadersDictionaryFromHeadersString):
[#2939](https://github.com/Starcounter/Starcounter/issues/2939).
- Fixed extraction of CRT libraries in installer GUI that caused the issue [#2759](https://github.com/Starcounter/Starcounter/issues/2759)
- Bug fixed when handling error from indexcreation, that caused an assertion failure instead of returning the error to usercode: [#2951](https://github.com/Starcounter/Starcounter/issues/2951) 
