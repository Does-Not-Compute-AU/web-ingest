# Developer Guide

### General Info

- The front-end client is written in BlazorWASM, and so can be hosted with any HTTP server. 
- By default, the WebAPI project will also run the front-end, and host it at https://localhost:5001

## Develop on GitPod
[![Open in Gitpod](https://gitpod.io/button/open-in-gitpod.svg)](https://gitpod.io/#https://github.com/Does-Not-Compute-AU/web-ingest)


## Develop on Local Machine
#### Install Prerequisites

- Dotnet Core SDK 6.0
- Docker
- IDE: VisualStudio / VSCode / Rider

#### Running Project

From terminal at root directory:
- `docker-compose up -d` // this will run the container services required for the program to run
- `dotnet run watch --project WebIngest.WebAPI/` // this will run the WebAPI project, which also hosts an instance of the UI


### Things To Know
- There is a `menu.sh` file in root directory which can be used for developer controls like updating migrations & db-reset.
- Database Migration script (entityframework) is executed by code in startup of WebAPI, no need to run ```dotnet ef database update```
- WebIngest.Common & WebIngest.Core are class libraries, they are not executable