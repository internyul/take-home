# qa-backend-code-challenge

Code challenge for QA Backend Engineer candidates.

### Build Docker image

Run this command from the directory where there is the solution file.

```
docker build -f src/Betsson.OnlineWallets.Web/Dockerfile .
```

### Run Docker container

```
docker run -p <port>:8080 <image id>
```

### Open Swagger

```
http://localhost:<port>/swagger/index.html
```
### Using `Makefile`:

```
make build         # dotnet build (Release)
make test          # dotnet test
make coverage      # dotnet test with coverage to ./TestResults
make docker-test   # docker build --target test (runs tests in Docker)
make docker-build  # docker build production image
make docker-run    # run image (maps PORT=8080 by default)
```