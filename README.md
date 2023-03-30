# market-service

## Introduction
This repository provide market product list service for Nine Chronicles

## Installation
- [.NET6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/get-started/overview/install)

## How to run

### Set environment

you can edit launchSettings.json for run configuration and run service.

| VALUE                     | DESCRIPTION                                                |
|---------------------------|------------------------------------------------------------|
| ConnectionStrings__MARKET | database path for market service                           |
| RpcConfig__Host           | NineChronicles.Headless node host for sync market products |
| RpcConfig__Port           | NineChronciles.Headless node port                          |
| WorkerConfig__SyncShop    | if true, sync registered ShardedShopStateV2 Orders         |
| WorkerConfig__SyncProduct | if true, sync registered MarketState Products              |

```shell
$ dotnet run --project MarketService
```
