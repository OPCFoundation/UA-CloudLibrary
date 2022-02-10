#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["UACloudLibraryServer/UA-CloudLibrary.csproj", "UACloudLibraryServer/"]
RUN dotnet restore "UACloudLibraryServer/UA-CloudLibrary.csproj"
COPY . .
WORKDIR "/src/UACloudLibraryServer"
RUN dotnet build "UA-CloudLibrary.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UA-CloudLibrary.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UA-CloudLibrary.dll"]