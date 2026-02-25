FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MediMate/MediMate.csproj                           MediMate/
COPY MediMateRepository/MediMateRepository.csproj      MediMateRepository/
COPY MediMateService/MediMateService.csproj             MediMateService/
COPY Share/Share.csproj                                 Share/

RUN dotnet restore MediMate/MediMate.csproj

COPY MediMate/           MediMate/
COPY MediMateRepository/ MediMateRepository/
COPY MediMateService/    MediMateService/
COPY Share/              Share/

FROM build AS publish
WORKDIR /src
RUN dotnet publish MediMate/MediMate.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "MediMate.dll"]
