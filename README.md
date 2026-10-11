# 2026 MAUI Blazor Hybrid Workshop Repository

This repository holds all of the items necessary to follow-along, and complete the activities outlined as part of the 2026 Full-Day Workshop delivered by Mitchel Sellers at Tech Bash 2026.

>[!NOTE]
>The steps, instructions, and templates contained within were re-validated on 10/10/2026 using the "then current" versions of Visual Studio and MAUI Blazor Hybrid, it is possible that changes have occured that may make this no longer valid.

## Lab Prerequisites

Before starting the lab the following validations are recommended

- Install .NET 10 SDK and the .NET MAUI workload; verify with `dotnet --info` and `dotnet workload list`.
- Setup an Android Emulator within Visual Studio (We will show this within the course)
- If unable to setup Android emulator, the Windows target can be utilized as a stop-gap

## Navigating This Repository

Within the "Docs" folder you will find the full instructions, including ALL code/steps that are needed within the `docs/xxx-xxx.md` files.

A tag was created at the end of each of the project steps, you can 

## High Level Steps Summary

The table below outlines the high-level steps for the day

| Lab | Approx. time | Notes |
|---|---:|---|---|
| 001 | 45m | Host-to-WebView chain |
| 002 | 45m | Contracts and registration |
| 003 | 55m | Card component and route |
| 004 | 65m | Slow data, search, errors |
| 005 | 50m | Start API, test `/health`, switch mobile to API
| 006 | 45m | Edit order, show invalid summary, save
| 007 | 45m | Connectivity, photo picker, saved preference
| 008 | 45m | Edit locally, view pending count, sync - Cache populated before going offline; no concurrency guarantees |
| 009 | 45m | Secure storage and bearer handler |
| 010 | 30m | Diagnostic timing and release checklist |

## Limitation of Scope

To help keep the demonstrations valuable, we have purposfully skipped over authentication.