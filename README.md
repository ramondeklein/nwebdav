# NWebDAV
.NET implementation of a WebDAV server.

## Overview
I needed a WebDAV server implementation for C#, but I couldn't find an
implementation that fulfilled my needs. That's why I wrote
my own.

__Requirements__

* Fast, scalable, robust with moderate memory usage.
* Abstract data store, so it can be used for directories/files but also for any
  other data.
* Abstract locking providers, so it can use in-memory locking (small servers)
  or Redis (large installations).
* Flexible and extensible property management.
* Fully supports .NET framework, Mono and the Core CLR.
* Allows for various HTTP authentication and SSL support (basic authentication works).

## Work in progress
This module is currently work-in-progress and shouldn't be used for production use yet. If you want to help, then let me know...
The following features are currently missing:

* Only the in-memory locking provider has been implemented yet.
* Check if each call responds with the proper status codes (as defined in the WebDAV specification).
* Recursive locking is not supported yet.

The current version seems to work fine to serve files using WebDAV on both Windows and OS X.
