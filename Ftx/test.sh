#!/bin/bash
echo 准备安装服务
cd /data/github/FtxApi/
git pull
docker run --rm -v /data/github/FtxApi/:/app -w /app mcr.microsoft.com/dotnet/sdk:5.0 dotnet publish -c Release /app/Ftx/Ftx.csproj
docker run --rm -v /data/github/FtxApi/Ftx/bin/Release/net5.0/publish/:/app -w /app mcr.microsoft.com/dotnet/runtime:5.0 dotnet Ftx.dll