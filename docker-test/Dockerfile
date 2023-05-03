FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
COPY Mastersign.ConfigModel.sln /app/Mastersign.ConfigModel.sln
COPY Mastersign.ConfigModel /app/Mastersign.ConfigModel
COPY Mastersign.ConfigModel.Test /app/Mastersign.ConfigModel.Test
COPY Mastersign.ConfigModel.WatchTest /app/Mastersign.ConfigModel.WatchTest
WORKDIR /app
RUN dotnet build Mastersign.ConfigModel.WatchTest/Mastersign.ConfigModel.WatchTest.csproj \
    -c Debug -o /app/bin

FROM mcr.microsoft.com/dotnet/runtime:6.0
COPY --from=build /app/bin/* /app/
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
ENTRYPOINT ["dotnet", "/app/Mastersign.ConfigModel.WatchTest.dll"]
