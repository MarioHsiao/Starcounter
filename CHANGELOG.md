## [Unreleased][unreleased]
### Added
- New staradmin command `staradmin start server`, as requested in [#2950](https://github.com/Starcounter/Starcounter/issues/2950) and documented at [staradmin CLI](http://starcounter.io/guides/tools/staradmin/).
- New staradmin command `staradmin start database`, as requested in [#2950](https://github.com/Starcounter/Starcounter/issues/2950) and documented at [staradmin CLI](http://starcounter.io/guides/tools/staradmin/). By default, this command also automatically create the specified database if it does not exist.
- Support for new command `staradmin new db` allowing simple creation of databases from the command-line, as requested in [#2973](https://github.com/Starcounter/Starcounter/issues/2973) and documented at [staradmin CLI](http://starcounter.io/guides/tools/staradmin/).
- Added support for new command option --failmissing to `staradmin delete`, forcing the operation to fail if the specified artifact does not exist, as described in [#2995](https://github.com/Starcounter/Starcounter/issues/2995). Documented at [staradmin CLI](http://starcounter.io/guides/tools/staradmin/).
- Support for transient classes, [#3010](https://github.com/Starcounter/Starcounter/issues/3010).
- New feature: assembly level [Database] declaration, [#3005](https://github.com/Starcounter/Starcounter/issues/3005).
- Simplifications when passing source code to `star.exe`, [#3004](https://github.com/Starcounter/Starcounter/issues/3004) and [#3011](https://github.com/Starcounter/Starcounter/issues/3011).
- Introduced support for transacted entrypoints with `star --transact`, [#3008](https://github.com/Starcounter/Starcounter/issues/3008).
- Introduced simple built-in dependency injection into Starcounter and in the code host in particular, enabled by IServices and IServiceContainer, outlined by [#3017](https://github.com/Starcounter/Starcounter/issues/3017)
- Added support for first *extension point* in Starcounter, based on new `IQueryRowsResponse` interface as issued in [#3016](https://github.com/Starcounter/Starcounter/issues/3016)
- Introduced a small suite of classes allowing simple **iteration of property values** using class `ViewReader`, described in [#3033](https://github.com/Starcounter/Starcounter/issues/3033).
- Upgraded client side libraries (list of current versions available in src/BuildSystem/ClientFiles/bower-list.txt)
- Ability to specify multiple resource directories on the command-line, fixes [#2898](https://github.com/Starcounter/Starcounter/issues/2898). For reference, see [#3099](https://github.com/Starcounter/Starcounter/issues/3099).
- `Partial` class with the support for implicit standalone mode [#3176](https://github.com/Starcounter/Starcounter/issues/3176)
- Added possibility to use straight handlers paramters notation "{?}" in URIs when doing mapping. Paramter type notation "@w" is still supported but is temporary and will be removed in future.
- Added functionality to unregister existing HTTP handlers. Documentation information added to http://starcounter.io/guides/network/handling-requests/#unregistering-existing-http-handlers
- Added a possibility to stream data over TCP, WebSockets and HTTP responses: [#9](https://github.com/Starcounter/Starcounter/issues/9)
- Added `Session.ToAsciiString()` to convert an existing session into an ASCII string. Later this session ASCII string can be used as parameter to `Session.ScheduleTask`.
- Added simpler task scheduling interface using static method `Scheduling.ScheduleTask()`.
- Added excecptions with information about failed table to upgrade. Related to [#3383](https://github.com/Starcounter/Starcounter/issues/3383) and [#3368](https://github.com/Starcounter/Starcounter/issues/3368).
- Extended the basic admin REST API to support creating databases with custom settings [#3362](https://github.com/Starcounter/Starcounter/issues/3362)
- Made `staradmin new db` support the name to be given as a first parameter, like `staradmin new db`
- Extended `staradmin new db` to support custom settings as specified in [#3360](https://github.com/Starcounter/Starcounter/issues/3360)
- Introduced new IMiddleware class and the new consolidated middleware Application.Use() API's, as described in See [#3296](https://github.com/Starcounter/Starcounter/issues/3296)
- Extended weaver diagnostics emitted by `scweaver --verbosity=diagnostic according to [#3420](https://github.com/Starcounter/Starcounter/issues/3420)
- Introduced support to provision HTML (views) from JSON (view models) by means of middleware. See [#3444](https://github.com/Starcounter/Starcounter/issues/3444)
- Added possibility to register internal codehost handlers with `HandlerOptions.SelfOnly`. See [#3339](https://github.com/Starcounter/Starcounter/issues/3339)
- Added overloads for `Db.Transact` that allows specifying delegates that take input and output parameters. See [#2822](https://github.com/Starcounter/Starcounter/issues/2822) and documentation on http://starcounter.io/guides/transactions/ 

### Fixed
- Bug fixed for inheritance of objects and arrays in TypedJSON that caused null references: [#2955](https://github.com/Starcounter/Starcounter/issues/2955)
- Fixed issue with setting outgoing fields and using outgoing filters in relation to static file resources responses: [#2961](https://github.com/Starcounter/Starcounter/issues/2961).
- Fixed issue with missing AppName and PartialId in serialized json when running Launcher: [#2902](https://github.com/Starcounter/Starcounter/issues/2902)
- Fixed an issue when Administrator was starting faster than gateway process in scservice: [#2962](https://github.com/Starcounter/Starcounter/issues/2962)
- Fixed text input and text selection issues in Administrator [#2942](https://github.com/Starcounter/Starcounter/issues/2942), [#2400](https://github.com/Starcounter/Starcounter/issues/2400), [#1993](https://github.com/Starcounter/Starcounter/issues/1993)
- Fixed max column width issue in Administrator [#2828](https://github.com/Starcounter/Starcounter/issues/2828)
- Fixed incorrect invalidation of databinding for bound properties in TypedJSON: [#2998](https://github.com/Starcounter/Starcounter/issues/2998)
- Fixed bug caused by using synonyms in new builds: [#2997](https://github.com/Starcounter/Starcounter/issues/2997)
- Removed (not implemented) option `staradmin delete log` as decided in [#2974](https://github.com/Starcounter/Starcounter/issues/2974).
- Fixed [#2976](https://github.com/Starcounter/Starcounter/issues/2976), resource directories and the working directory are no longer mixed.
- Fixed issue with patches for items in arrays for TypedJson sometimes having incorrect index.
- Fixed matching metadata-properties with regular properties in JSON-by-example [#3136](https://github.com/Starcounter/Starcounter/issues/3136).  
- Fixed reseting URL to `""` in view-model after `<juicy-redirect>`/`<puppet-redirect>` redirect [PuppetJs/puppet-redirect#1](https://github.com/PuppetJs/puppet-redirect/issues/1), [PuppetJs/puppet-redirect#2](https://github.com/PuppetJs/puppet-redirect/issues/2)
- Serializing TypedJson from usercode no longer generates json with namespaces. Namespaces are only added when serializing the public viewmodel when the option is set in the session, and also when patches are generated with the same option set. [#3148](https://github.com/Starcounter/Starcounter/issues/3148)
- Improved diagnostic content when weaver is unable to resolve an application dependency, as outlined in [#3227](https://github.com/Starcounter/Starcounter/issues/3227). Now include the probably referring assembly.
- Fixed mouse and keyboard scrolling issues in Administrator error log and SQL browser [#2990](https://github.com/Starcounter/Starcounter/issues/2990), [#2987](https://github.com/Starcounter/Starcounter/issues/2987), [#2986](https://github.com/Starcounter/Starcounter/issues/2986), [#1635](https://github.com/Starcounter/Starcounter/issues/1635)
- Fixed nullreference exception in some cases when a bound array in TypedJSON was changed [#3245](https://github.com/Starcounter/Starcounter/issues/3245)
- Fixed correct handling of bound values for arrays in TypedJSON when bound value was null [#3304](https://github.com/Starcounter/Starcounter/issues/3304)
- Wrapping all generated classes for TypedJSON inside namespace to avoid clashing of names [#3316](https://github.com/Starcounter/Starcounter/issues/3316)
- Added verification when generating code from JSON-by-example for TypedJSON to make sure all properties only contains valid characters [#3103](https://github.com/Starcounter/Starcounter/issues/3103)
- Wrapped unhandled exception from a scheduled task inside a starcounter exception to preserve stacktrace [#3032](https://github.com/Starcounter/Starcounter/issues/3032), [#3122](https://github.com/Starcounter/Starcounter/issues/3122), [#3329](https://github.com/Starcounter/Starcounter/issues/3032)
- Assured weaver.ignore expressions with leading/trailing whitespaces are trimmed, as defined in [#3414](https://github.com/Starcounter/Starcounter/issues/3414)
- Removed buggy dependency to custom VS output pane "Starcounter" as described in [#3423](https://github.com/Starcounter/Starcounter/issues/3423)
- Shaped up obsolete status terminology used in VS as reported in [#532](https://github.com/Starcounter/Starcounter/issues/532)
- Fixed the problem with ScErrInputQueueFull exception when scheduling tasks [#3388](https://github.com/Starcounter/Starcounter/issues/3388)
- Fixed sending only changed/added siblings instead of all siblings when sending patches to client. [#3465](https://github.com/Starcounter/Starcounter/issues/3465)   
- Fixed a potential problem with long-running transactions and scheduling a task for a session that used the same scheduler. [#3472](https://github.com/Starcounter/Starcounter/issues/3472)

### Changed
- Changed so that working directory is no longer a resource directory by default.
- Changed so that implicit resource directories are discovered based on the working directory.
- Renamed the MiddlewareFiltersEnabled database flag to RequestFiltersEnabled.
- Its no longer possible to register handlers with same signature. For example, one can't register handler "GET /{?}" with string parameter, and handler "GET /{?}" with integer parameter.
- Due to [`<juicy-redirect>`](https://github.com/Juicy/juicy-redirect) and [`<puppet-redirect>`](https://github.com/PuppetJs/puppet-redirect) update, Custom Element should now be imported from `/sys/juicy-redirect/juicy-redirect.html` or `/sys/puppet-redirect/puppet-redirect.html`. When used with Polymer's template binding, `url` value can be bound two-way via property: `<juicy-redirect url="{{model.path.to.RedirectURL$}}">`
- Added method(s) on Session taking a delegate to be run instead of using `session.StartUsing()` and `session.StopUsing()`,  these two methods are no longer public. [#3117](https://github.com/Starcounter/Starcounter/issues/3117)
- Session API has been refactored. New `Session.ScheduleTask` is added. `Session.ForAll` has been refactored.
- `Session.Destroyed` is now replaced by `Session.AddDestroyDelegate` because of apps separation issues.
- `Session.CargoId` is removed because of no use.
- Made Handle.AddRequestFilter and Handle.AddResponseFilter obsolete in favor new Application.Use() API. See [#3296](https://github.com/Starcounter/Starcounter/issues/3296)
- Syntax for getting headers in request and response changed from `r["Headername"]` to `r.Headers["Headername"]`.
- Changed API for getting all headers string to `r.GetAllHeaders()` for both request and response.
- In `Http` and `Node` receive timeout parameter has changed from milliseconds to seconds (no reasons to have it with milliseconds precision). 
- In `Http` and `Node` functions the `userObject` parameter is gone. Because of that the `userDelegate` parameter which was previously `Action<Response, Object>` became just `Action<Response>`.
- Static files from /sys/ folder migrated to Polymer 1.3. See [#3384](https://github.com/Starcounter/Starcounter/issues/3384), [#3022](https://github.com/Starcounter/Starcounter/issues/3022)
- Deprecate usage of `<dom-bind-notifier>` in HTML partial templates. Since Polymer 1.3 upgrade, it is not needed. See [#2922](https://github.com/Starcounter/Starcounter/issues/2922)
- Deprecate usage of `Object.observe` and `Array.observe` shims. Since Polymer 1.3 upgrade, they are not needed. [#3468](https://github.com/Starcounter/Starcounter/issues/3468)
- Deprecate usage of `juicy-redirect`. Puppet web apps should use `puppet-redirect` instead. [#3469](https://github.com/Starcounter/Starcounter/issues/3469)

## [2.1.177] - 2015-10-14
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
