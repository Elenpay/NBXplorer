FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0.202-noble AS builder
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates && rm -rf /var/lib/apt/lists/*
# The SDK's NuGet signing bundles still trust the roots behind the compromised eMudhra intermediates.
# Drop this step once the SDK ships its own fix.
RUN for f in /usr/share/dotnet/sdk/*/trustedroots/*.pem; do \
      d=$(mktemp -d) && csplit -s -z -f "$d/c" "$f" '/BEGIN CERTIFICATE/' '{*}' && : > "$f" && \
      for c in "$d"/c*; do openssl x509 -in "$c" -noout -subject | grep -qE 'CN ?= ?(AAA Certificate Services|emSign)' || cat "$c" >> "$f"; done; \
      rm -rf "$d"; \
    done
WORKDIR /source
COPY NBXplorer/NBXplorer.csproj NBXplorer/NBXplorer.csproj
COPY NBXplorer.Client/NBXplorer.Client.csproj NBXplorer.Client/NBXplorer.Client.csproj
# Cache some dependencies
RUN cd NBXplorer && dotnet restore && cd ..
COPY . .
RUN cd NBXplorer && \
    dotnet publish --output /app/ --configuration Release

FROM mcr.microsoft.com/dotnet/aspnet:10.0.14-noble
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates && rm -rf /var/lib/apt/lists/*
WORKDIR /app

RUN mkdir /datadir
ENV NBXPLORER_DATADIR=/datadir
VOLUME /datadir

COPY --from=builder "/app" .
ENTRYPOINT ["dotnet", "NBXplorer.dll"]