FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["MeuDinDin/MeuDinDin.csproj", "MeuDinDin/"]
RUN dotnet restore "MeuDinDin/MeuDinDin.csproj"

COPY . .
WORKDIR /src/MeuDinDin
RUN dotnet publish "MeuDinDin.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV MEUDINDIN_DATA_DIR=/app/App_Data

RUN mkdir -p /app/App_Data

EXPOSE 8080
VOLUME ["/app/App_Data"]

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MeuDinDin.dll"]
