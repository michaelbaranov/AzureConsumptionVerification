# AzureConsumptionVerification

Verifies Azure Bills for presence of deleted resources

Requires service pricipal for Authentication, see [here](https://docs.microsoft.com/en-us/cli/azure/create-an-azure-service-principal-azure-cli?view=azure-cli-latest)

Service principal can be created with az CLI
```
az ad sp create-for-rbac --name ServicePrincipalName
```
To build tool navigate to azureconsumptionverification and run
```
dotnet build
```

```
Expected parameters:
 -clientId
 -clientSecret
 -tenantId
 -subscription [mandatory] supported values - <all>, <array of subscription ids>
 -numberOfMonths [optional, default 1] number of months to analyze, due to Activity log API limitation in 90 days max value is 4
 -outputFolder [optional, default %TEMP%] folder to save report
 -openReport [optional, default <empty>] switch if enabled opens generated report
 -onlyWithOverages [optional, default <empty>] switch if enabled show only resources with overages
 ```

Examples:
Run analysis for all subscriptions available for service principal, show only those with overages
``` 
AzureConsumptionVerification -clientId=124d8317-dd0a-47f8-b630-c4839eb1602d -clientSecret=ObTY9A53gEB3_TgUFICK=gqX_NedhlE- -tenantId=91700184-c314-4dc9-bb7e-a411df456a1e -subscription=all -outputFolder="c:\reports" -openReport -onlyWithOverages
```
Run analysis for single subscription
```
AzureConsumptionVerification -clientId=124d8317-dd0a-47f8-b630-c4839eb1602d -clientSecret=ObTY9A53gEB3_TgUFICK=gqX_NedhlE- -tenantId=91700184-c314-4dc9-bb7e-a411df456a1e -subscription=22d1e318-4c86-4f8e-9cef-b04f36ba31c0 -outputFolder="c:\reports" -openReport
```
Run analysis for two subscriptions
```
AzureConsumptionVerification -clientId=124d8317-dd0a-47f8-b630-c4839eb1602d -clientSecret=ObTY9A53gEB3_TgUFICK=gqX_NedhlE- -tenantId=91700184-c314-4dc9-bb7e-a411df456a1e -subscription=22d1e318-4c86-4f8e-9cef-b04f36ba31c0,1e31822d-5c86-4a8e-9cef-f36ba31c0b04 -outputFolder="c:\reports" -openReport
```
