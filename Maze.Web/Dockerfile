#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
RUN apt-get update && apt-get install -y libfontconfig1

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Maze.Web/Maze.Web.csproj", "Maze.Web/"]
RUN dotnet restore "Maze.Web/Maze.Web.csproj"
COPY . .
WORKDIR "/src/Maze.Web"
RUN dotnet build "Maze.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Maze.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Maze.Web.dll"]
