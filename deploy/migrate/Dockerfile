# Migration image for cluster / CI one-shot jobs.
# Contains the repo checkout and runs deploy/migrate/migrate-all.sh
FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src

# Layer restore inputs first for better cache reuse when only app code changes.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY .config/ ./.config/
COPY src/ ./src/
COPY deploy/migrate/ ./deploy/migrate/

RUN chmod +x deploy/migrate/migrate-all.sh \
    && dotnet tool restore

ENV POSTGRES_HOST=postgres \
    POSTGRES_PORT=5432 \
    POSTGRES_USER=msf

ENTRYPOINT ["/bin/bash", "deploy/migrate/migrate-all.sh"]
