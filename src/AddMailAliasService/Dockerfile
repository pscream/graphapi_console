FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

COPY ./ ./
RUN dotnet restore && dotnet publish -c Release -o out

FROM mcr.microsoft.com/azure-powershell:7.1.0-ubuntu-18.04
RUN apt update && apt install wget
RUN wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb

RUN apt update; apt install -y apt-transport-https && apt update && apt install -y dotnet-runtime-3.1

RUN pwsh -Command Register-PackageSource -Trusted -ProviderName PowerShellGet -Name PoshTestGallery -Location "https://www.poshtestgallery.com/api/v2/"
RUN pwsh -Command Install-Module AzureAD.Standard.Preview -RequiredVersion 0.1.599.7 -Scope AllUsers -Force
RUN pwsh -Command Install-Module -Name ExchangeOnlineManagement -RequiredVersion 2.0.5 -Scope AllUsers -Force

WORKDIR /app

COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "AddMailAliasService.dll"]