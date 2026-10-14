FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["FoodOrderingSystem/FoodOrderingSystem.csproj", "FoodOrderingSystem/"]
RUN dotnet restore "FoodOrderingSystem/FoodOrderingSystem.csproj"

COPY . .
WORKDIR "/src/FoodOrderingSystem"

RUN dotnet publish "FoodOrderingSystem.csproj" \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FoodOrderingSystem.dll"]
