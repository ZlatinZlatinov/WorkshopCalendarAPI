# Use the official .NET 9 SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY CalendarAPI.csproj ./
RUN dotnet restore ./CalendarAPI.csproj

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet publish -c Release -o /app --no-restore

# Use the official .NET 9 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy the published output
COPY --from=build /app .

# Expose ports (HTTP and HTTPS)
EXPOSE 5000
EXPOSE 5001

# Set environment variables for ASP.NET Core
ENV ASPNETCORE_URLS="http://+:5000;https://+:5001"
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application
ENTRYPOINT ["dotnet", "CalendarAPI.dll"] 