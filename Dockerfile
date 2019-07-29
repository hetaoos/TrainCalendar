FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 80
ENV TZ=Asia/Shanghai

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["TrainCalendar/TrainCalendar.csproj", "TrainCalendar/"]

RUN dotnet restore "TrainCalendar/TrainCalendar.csproj"
COPY . .
WORKDIR "/src/TrainCalendar"
#RUN dotnet build "TrainCalendar.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "TrainCalendar.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "TrainCalendar.dll"]