<!--TemplateMetadata-->
## About
This page is a template used by the Starcounter build system to generate help pages for well-known errors. **Do not edit it if you don't know what you do and the consequences of the edit!**

## Recoqnized template parameters
The build system will generate help pages using the following template parameters:
* ```%%ErrorCode%%``` will be replaced with the numerical error code, i.e. "123".
* ```%%ErrorCodeHex%%``` will be replaced with the numerical error code in hex, i.e. "7B".
* ```%%ErrorCodeDecorated%%``` will be replaced with the decorated error code, i.e. "SCERR123".
* ```%%ErrorId%%``` will be replaced with the error code definition, i.e. "ScErrOneTwoThree".
* ```%%ErrorCategory%%``` will be replaced with the error category, i.e. "General".
* ```%%ErrorSeverity%%``` will be replaced with the error severity, i.e. "Error" or "Warning".
* ```%%ErrorMessage%%``` will be replaced with the error message, i.e. "Error 123 has occured".

Generated pages will be created with a name like ```%%ErrorId%%-(%%ErrorCodeDecorated%%)``` and hence have a title based on the same convention.

## Template
<!--TemplateMetadata/-->
<!--TemplateContent-->
Lorem ipsum.
<!--TemplateContent/-->