FROM mcr.microsoft.com/dotnet/sdk:7.0 AS base
WORKDIR /app

# should be a comma-delimited list
ENV CLUSTER_SEEDS "[]"
ENV CLUSTER_IP ""
ENV CLUSTER_PORT "5054"

COPY ./bin/Release/net7.0/publish/ /app

# 9110 - Petabridge.Cmd
# 5055 - Akka.Cluster
EXPOSE 9110 5054

# Install Petabridge.Cmd client
RUN dotnet tool install --global pbm 

# Needed because https://stackoverflow.com/questions/51977474/install-dotnet-core-tool-dockerfile
ENV PATH="${PATH}:/root/.dotnet/tools"

# RUN pbm help

CMD ["dotnet", "Akka.CQRS.TradePlacers.Service.dll"]