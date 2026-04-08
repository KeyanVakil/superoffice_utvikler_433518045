#!/usr/bin/env bash
set -euo pipefail

# MarketFlow ACI deployment — all secrets stay in Azure, never in git.

RG="marketflow-rg"
ACR="marketflowacr"
LOCATION="northeurope"
ACI_NAME="marketflow"
DNS_LABEL="marketflow-demo"
SA_PASSWORD="$(openssl rand -base64 24 | tr -d '/+=' | head -c 20)Aa1!"

echo "==> Creating resource group..."
az group create --name "$RG" --location "$LOCATION" --output none

echo "==> Creating ACR..."
az acr create --resource-group "$RG" --name "$ACR" --sku Basic --admin-enabled true --output none

echo "==> Building and pushing images..."
az acr build --registry "$ACR" --image marketflow-api:latest ./src/MarketFlow.Api/
az acr build --registry "$ACR" --image marketflow-web:latest ./web/

echo "==> Fetching ACR credentials..."
ACR_PASSWORD=$(az acr credential show --name "$ACR" --query "passwords[0].value" -o tsv)

echo "==> Deploying ACI container group..."
# Multi-container ACI requires YAML. Generate on-the-fly with secrets interpolated.
cat <<YAML | az container create --resource-group "$RG" --file /dev/stdin --output none
apiVersion: 2021-09-01
location: $LOCATION
name: $ACI_NAME
type: Microsoft.ContainerInstance/containerGroups
properties:
  osType: Linux
  restartPolicy: Always
  ipAddress:
    type: Public
    dnsNameLabel: $DNS_LABEL
    ports:
      - protocol: TCP
        port: 80
  imageRegistryCredentials:
    - server: ${ACR}.azurecr.io
      username: $ACR
      password: "$ACR_PASSWORD"
  containers:
    - name: db
      properties:
        image: mcr.microsoft.com/mssql/server:2022-latest
        resources:
          requests:
            cpu: 1.0
            memoryInGb: 2.0
        environmentVariables:
          - name: ACCEPT_EULA
            value: "Y"
          - name: MSSQL_SA_PASSWORD
            secureValue: "$SA_PASSWORD"
        ports: []

    - name: api
      properties:
        image: ${ACR}.azurecr.io/marketflow-api:latest
        resources:
          requests:
            cpu: 0.5
            memoryInGb: 1.0
        environmentVariables:
          - name: ConnectionStrings__DefaultConnection
            secureValue: "Server=localhost;Database=MarketFlow;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True"
          - name: ASPNETCORE_URLS
            value: "http://+:5000"
          - name: ASPNETCORE_ENVIRONMENT
            value: "Production"
        ports: []

    - name: web
      properties:
        image: ${ACR}.azurecr.io/marketflow-web:latest
        resources:
          requests:
            cpu: 0.25
            memoryInGb: 0.5
        environmentVariables:
          - name: API_UPSTREAM
            value: "localhost:5000"
        ports:
          - port: 80
YAML

echo ""
echo "==> Deployed! Waiting for IP..."
az container show --resource-group "$RG" --name "$ACI_NAME" \
  --query "ipAddress.fqdn" -o tsv

echo ""
echo "Site will be available at:"
echo "  http://${DNS_LABEL}.${LOCATION}.azurecontainer.io"
