# Use the .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy the CSPROJ file and restore any dependencies (via NUGET)
COPY *.csproj ./
RUN dotnet restore

# Copy the project files and build our release
COPY . ./
RUN dotnet publish -c Release -o out

# Generate the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build-env /app/out .

# Install pdflatex with additional packages including biblatex
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       texlive-latex-extra \
       texlive-fonts-recommended \
       texlive-bibtex-extra \
       biber \
    && rm -rf /var/lib/apt/lists/*

EXPOSE 80
EXPOSE 8080

ENTRYPOINT ["dotnet", "SimpLeX-Compiler.dll"]
