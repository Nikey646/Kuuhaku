FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /src

COPY ["src/Kuuhaku.sln", "./src/"]

COPY src/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p src/${file%.*}/ && mv $file src/${file%.*}/; done
RUN ls ./src/

RUN dotnet restore ./src/Kuuhaku.sln

COPY . ./
RUN dotnet publish -c Release -o out ./src/Kuuhaku.sln

RUN ls ./

FROM mcr.microsoft.com/dotnet/core/runtime:3.1
WORKDIR /src
COPY --from=build /src/out .
ENTRYPOINT ["dotnet", "Kuuhaku.dll"]
