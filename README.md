# NWebDAV
.NET implementation of the WebDAV protocol

## Overview
I needed a WebDAV server implementation for C#, but I couldn't find an implementation that fulfilled my needs. That's why I wrote
my own implementation.

__Requirements__

* Fast, scalable, robust with moderate memory usage.
* Abstract data store, so it can be used for directories/files but also for any other data.
* Flexible and extensible property management.
* Allows for various HTTP authentication and SSL support (not finished yet)

## Work in progress
This module is currently work-in-progress and shouldn't be used for production use yet. If you want to help, then let me know...
The following features are currently missing:

* Need some work returning the proper error codes (coming soon)
* Locking. I am still thinking about a flexible and scalable solution. I think I will use an in-memory locking database for single
  instance servers and a Redis-based locking database for distributed installations.
