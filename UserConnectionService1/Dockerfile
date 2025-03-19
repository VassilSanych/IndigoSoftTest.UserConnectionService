# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# These ARGs allow for swapping out the base used to make the final image when debugging from VS
ARG LAUNCHING_FROM_VS
# This sets the base image for final, but only if LAUNCHING_FROM_VS has been defined
ARG FINAL_BASE_IMAGE=${LAUNCHING_FROM_VS:+aotdebug}

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["UserConnectionService/UserConnectionService.csproj", "UserConnectionService/"]
RUN dotnet restore "UserConnectionService/UserConnectionService.csproj"
COPY . .
WORKDIR "/src/UserConnectionService"
RUN dotnet build "UserConnectionService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UserConnectionService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserConnectionService.dll"]