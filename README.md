# Developer Guide

## Prerequisites

- Dotnet core sdk 6.0
- Docker
- IDE: VisualStudio / VSCode / Rider

## Dev Links
- WebAPI: https://localhost:5001

## Scripts
Use terminal to execute "menu.sh" for developer environment controls

## General Info

Setup is straightforward. Clone repo, open .sln file with IDE of choice (built with Jetbrains Rider) and Build solution.

This should restore any required nuget packages and build executables.

###
**Run Docker Project First**, this provides db containers required by project to run.

Run / Debug with IDE is recommended, but you could run respective projects using ```dotnet run``` from within the project folder

## Notes

- Make sure you have the appropriate containers running to support the application, "Start Devstack" option can be found in menu.sh file for ensuring this.
- Database Migration script (entityframework) is executed by code in startup of WebAPI, no need to run ```dotnet ef database update``` or similar
- WebIngest.Common & WebIngest.Core are class libraries, they are not executable