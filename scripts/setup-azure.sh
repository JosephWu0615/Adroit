#!/bin/bash

# =============================================================================
# Azure Infrastructure Setup Script for Adroit URL Shortener
# =============================================================================
#
# Prerequisites:
# 1. Azure CLI installed and logged in (az login)
# 2. Subscription set (az account set --subscription <subscription-id>)
#
# Usage:
#   chmod +x setup-azure.sh
#   ./setup-azure.sh
# =============================================================================

set -e

# Configuration
RESOURCE_GROUP="rg-adroit"
LOCATION="eastus"
APP_NAME="adroit-api"
SQL_SERVER_NAME="adroit-sql-server"
SQL_DB_NAME="adroit-db"
STATIC_WEB_APP_NAME="adroit-web"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== Adroit Azure Infrastructure Setup ===${NC}"
echo ""

# Check if logged in
echo -e "${YELLOW}Checking Azure CLI login...${NC}"
az account show > /dev/null 2>&1 || { echo -e "${RED}Please login first: az login${NC}"; exit 1; }

# Get subscription info
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
SUBSCRIPTION_NAME=$(az account show --query name -o tsv)
echo -e "${GREEN}Using subscription: $SUBSCRIPTION_NAME ($SUBSCRIPTION_ID)${NC}"
echo ""

# Prompt for SQL admin password
echo -e "${YELLOW}Enter SQL Server admin password (min 8 chars, must include uppercase, lowercase, number):${NC}"
read -s SQL_ADMIN_PASSWORD
echo ""

# =============================================================================
# 1. Create Azure SQL Server and Database
# =============================================================================
echo -e "${YELLOW}[1/4] Creating Azure SQL Server...${NC}"

az sql server create \
    --name $SQL_SERVER_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --admin-user sqladmin \
    --admin-password "$SQL_ADMIN_PASSWORD"

echo -e "${GREEN}SQL Server created: $SQL_SERVER_NAME${NC}"

# Allow Azure services to access SQL Server
echo -e "${YELLOW}Configuring SQL Server firewall...${NC}"
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name AllowAzureServices \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0

# Create SQL Database
echo -e "${YELLOW}Creating SQL Database...${NC}"
az sql db create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name $SQL_DB_NAME \
    --service-objective Basic

echo -e "${GREEN}SQL Database created: $SQL_DB_NAME${NC}"

# Get connection string
CONNECTION_STRING=$(az sql db show-connection-string \
    --server $SQL_SERVER_NAME \
    --name $SQL_DB_NAME \
    --client ado.net \
    --output tsv | sed "s/<username>/sqladmin/g" | sed "s/<password>/$SQL_ADMIN_PASSWORD/g")

echo ""

# =============================================================================
# 2. Create App Service Plan and Web App
# =============================================================================
echo -e "${YELLOW}[2/4] Creating App Service Plan...${NC}"

az appservice plan create \
    --name "${APP_NAME}-plan" \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --sku B1 \
    --is-linux

echo -e "${YELLOW}Creating Web App...${NC}"
az webapp create \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --plan "${APP_NAME}-plan" \
    --runtime "DOTNETCORE:8.0"

echo -e "${GREEN}Web App created: $APP_NAME${NC}"

# Configure connection string
echo -e "${YELLOW}Configuring connection string...${NC}"
az webapp config connection-string set \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --settings DefaultConnection="$CONNECTION_STRING" \
    --connection-string-type SQLAzure

# Configure app settings
az webapp config appsettings set \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --settings \
        ASPNETCORE_ENVIRONMENT=Production \
        AppSettings__BaseUrl="https://${APP_NAME}.azurewebsites.net" \
        AppSettings__FrontendUrl="https://${STATIC_WEB_APP_NAME}.azurestaticapps.net"

echo ""

# =============================================================================
# 3. Create Static Web App for Frontend
# =============================================================================
echo -e "${YELLOW}[3/4] Creating Static Web App...${NC}"

az staticwebapp create \
    --name $STATIC_WEB_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION \
    --sku Free

echo -e "${GREEN}Static Web App created: $STATIC_WEB_APP_NAME${NC}"

# Get deployment token
SWA_TOKEN=$(az staticwebapp secrets list \
    --name $STATIC_WEB_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --query "properties.apiKey" -o tsv)

echo ""

# =============================================================================
# 4. Get Publish Profile for App Service
# =============================================================================
echo -e "${YELLOW}[4/4] Getting deployment credentials...${NC}"

az webapp deployment list-publishing-profiles \
    --name $APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --xml > publish-profile.xml

echo -e "${GREEN}Publish profile saved to: publish-profile.xml${NC}"

# =============================================================================
# Summary
# =============================================================================
echo ""
echo -e "${GREEN}==============================================================================${NC}"
echo -e "${GREEN}                    Azure Infrastructure Setup Complete!                      ${NC}"
echo -e "${GREEN}==============================================================================${NC}"
echo ""
echo -e "${YELLOW}Resources Created:${NC}"
echo "  - SQL Server: $SQL_SERVER_NAME.database.windows.net"
echo "  - SQL Database: $SQL_DB_NAME"
echo "  - App Service: https://${APP_NAME}.azurewebsites.net"
echo "  - Static Web App: https://${STATIC_WEB_APP_NAME}.azurestaticapps.net"
echo ""
echo -e "${YELLOW}GitHub Secrets to Add:${NC}"
echo ""
echo "1. AZURE_WEBAPP_PUBLISH_PROFILE:"
echo "   Copy the content of publish-profile.xml"
echo ""
echo "2. AZURE_STATIC_WEB_APPS_API_TOKEN:"
echo "   $SWA_TOKEN"
echo ""
echo "3. SQL_CONNECTION_STRING:"
echo "   $CONNECTION_STRING"
echo ""
echo -e "${YELLOW}Next Steps:${NC}"
echo "1. Add the above secrets to your GitHub repository"
echo "2. Push your code to trigger the CI/CD pipeline"
echo "3. Visit your app at https://${APP_NAME}.azurewebsites.net"
echo ""
