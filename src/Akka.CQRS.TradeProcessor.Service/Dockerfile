FROM mcr.microsoft.com/dotnet/sdk:6.0 AS base
WORKDIR /app

# should be a comma-delimited list
ENV CLUSTER_SEEDS "[]"
ENV CLUSTER_IP ""
ENV CLUSTER_PORT "5055"
ENV SQL_CONNECTION_STR "" # SQL connection string for Akka.Persistence.Sql
ENV SQL_PROVIDER_NAME "" # SQL provider for Akka.Persistence.Sql

COPY ./bin/Release/net6.0/publish/ /app

# 9110 - Petabridge.Cmd
# 5055 - Akka.Cluster
EXPOSE 9110 5055

# Install Petabridge.Cmd client
RUN dotnet tool install --global pbm 

# Needed because https://stackoverflow.com/questions/51977474/install-dotnet-core-tool-dockerfile
ENV PATH="${PATH}:/root/.dotnet/tools"

# RUN pbm help

CMD ["dotnet", "Akka.CQRS.TradeProcessor.Service.dll"]