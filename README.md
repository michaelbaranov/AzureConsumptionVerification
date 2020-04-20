# AzureConsumptionVerification

Verifies Azure Bills for presence of deleted resources

Requires service pricipal for Authentication, see [here](https://docs.microsoft.com/en-us/cli/azure/create-an-azure-service-principal-azure-cli?view=azure-cli-latest)

Expected parameters:

 ```
 -clientId 
 -clientSecret
 -tenantId
 -subscriptionId
 -numberOfMonths [optional, default 1] number of months to analyze, 
 due to Activity log API limitation in 90 days max value is 4
 ```
 
Example:
``` 
AzureConsumptionVerification -cilentId=124d8317-dd0a-47f8-b630-c4839eb1602d -clientSecret=ObTY9A53gEB3_TgUFICK=gqX_NedhlE- -tenantId=91700184-c314-4dc9-bb7e-a411df456a1e -subscriptionId=38cadfad-6513-4396-af97-8606962edfa1
```
