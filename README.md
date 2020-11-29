# Sourcelinks
Sample Azure Functions Project

The Sourcelinks project is a skeleton that shows off how one can create API endpoints using Azure Functions and Azure (noSQL) Storage.  

This project includes an example that returns JSON, adjusts headers and routes, supports custom error handling along with custom logging.

The fuctionality offered in this API is kind of a poor man's API management tool.  The endpoint supplies, when given a "?target=<name>" parameter, a URL that can be used to access other resources.  This is meant to provide a single point one can find the source of the resoruce.  For clients like mobile apps, that can refuse updates, it supplies some flexibility in that they can use this call to find out where all the other APIs are located.  The idea is that this API would need to be long lived, but the larger API surface area could be moved around
