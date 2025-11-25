FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Bank/Bank.csproj", "Bank/"]
RUN dotnet restore "Bank/Bank.csproj"
COPY . .
WORKDIR "/src/Bank"
RUN dotnet build "Bank.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Bank.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Bank.dll"]
