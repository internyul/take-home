FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# ASP.NET 8 containers listen on 8080 by default via ASPNETCORE_URLS
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /sln
COPY ["src/Betsson.OnlineWallets.Web/Betsson.OnlineWallets.Web.csproj", "sln/Betsson.OnlineWallets.Web/"]

RUN dotnet restore "sln/Betsson.OnlineWallets.Web/Betsson.OnlineWallets.Web.csproj"
COPY . .
WORKDIR "/sln/src/Betsson.OnlineWallets.Web"
RUN dotnet build "Betsson.OnlineWallets.Web.csproj" -c Release -o /app/build

FROM build AS test
# Run the entire solution's tests with coverage collection.
# Building with --target test will execute these and fail on test failures.
WORKDIR /sln
RUN dotnet test Betsson.OnlineWallets.sln \
	-c Release \
	--collect:"XPlat Code Coverage" \
	--logger "trx;LogFileName=test_results.trx" \
	--results-directory /sln/TestResults \
	--verbosity minimal

FROM build AS publish
RUN dotnet publish "Betsson.OnlineWallets.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Betsson.OnlineWallets.Web.dll"]