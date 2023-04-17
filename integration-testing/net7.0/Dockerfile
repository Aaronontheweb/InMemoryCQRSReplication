FROM mcr.microsoft.com/dotnet/sdk:7.0 AS base
WORKDIR /app

COPY bin/nuget /nuget

# Install Petabridge.Cmd client so it can be invoked remotely via
# Docker or K8s 'exec` commands
RUN dotnet tool install --global --add-source /nuget pbm 

FROM mcr.microsoft.com/dotnet/runtime:7.0 AS app
WORKDIR /app

# copy .NET Core global tool
COPY --from=base /root/.dotnet /root/.dotnet/

# Needed because https://stackoverflow.com/questions/51977474/install-dotnet-core-tool-dockerfile
ENV PATH="${PATH}:/root/.dotnet/tools"

ENTRYPOINT ["pbm"]
CMD []