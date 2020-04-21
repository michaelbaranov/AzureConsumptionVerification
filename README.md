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
To run analysis execute following command from the shell in the same directory as dotnet build
```
dotnet run -cilentId=<service principal client id> -clientSecret=<service principal client secret> -tenantId=<service principal tenant id> -subscriptionId=<subsctiption to ananyze> -numberOfMonths=<[optional, default 1] number of months to analyze, 
 due to Activity log API limitation in 90 days max value is 4>
 ```

Example:
``` 
dotnet run -cilentId=124d8317-dd0a-47f8-b630-c4839eb1602d -clientSecret=ObTY9A53gEB3_TgUFICK=gqX_NedhlE- -tenantId=91700184-c314-4dc9-bb7e-a411df456a1e -subscriptionId=38cadfad-6513-4396-af97-8606962edfa1
```
