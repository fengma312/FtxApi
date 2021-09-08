#!/bin/bash
echo 准备安装服务
cd /data/project/RobotTrading/
git pull
docker stop coin
docker run --rm -v /data/github/:/app -w /app mcr.microsoft.com/dotnet/sdk:3.1 dotnet publish -c Release /app/RobotTrading/Com.Web/Com.Web.csproj
docker start coin
#docker run -d -p 8081:80 --restart=always --name coin --add-host=preapi.idcm.io:192.168.1.20 --add-host=prepush.idcm.io:192.168.1.51 --add-host=prereal.idcm.io:192.168.1.32 -e TZ=Asia/Shanghai -e ASPNETCORE_ENVIRONMENT=Staging -v /usr/share/fonts:/usr/share/fonts -v /data/github/RobotTrading/Com.Web/bin/Release/netcoreapp3.1/publish/:/app -w /app mcr.microsoft.com/dotnet/aspnet:3.1 dotnet Com.Web.dll