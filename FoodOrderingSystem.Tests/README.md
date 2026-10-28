# Test

Dalla root del repository:

```powershell
dotnet test FoodOrderingSystem.Tests/FoodOrderingSystem.Tests.csproj
```

I test locali usano SQLite in memoria. I tre test di integrazione SQL Server
vengono saltati se `SQLSERVER_TEST_CONNECTION_STRING` non è impostata.

Per verificare anche la concorrenza e il rollback su SQL Server:

```powershell
docker compose up -d database
$env:SQLSERVER_TEST_CONNECTION_STRING = 'Server=localhost,1433;User Id=sa;Password=<password locale>;TrustServerCertificate=True'
dotnet test FoodOrderingSystem.Tests/FoodOrderingSystem.Tests.csproj
Remove-Item Env:SQLSERVER_TEST_CONNECTION_STRING
```

Sostituire il segnaposto con la password del database locale senza salvare
la stringa nel repository. Il login deve poter creare ed eliminare database.
Ogni test SQL Server usa un database con nome univoco
`FoodOrderingCheckoutTests_<guid>`, eliminato al termine. `FoodOrderingDB`
non viene modificato.

Il test concorrente sincronizza due checkout dopo la lettura del carrello e
prima dell'aggiornamento dello stock: entrambi leggono l'ultimo pezzo, ma solo
uno deve completare l'ordine. Il carrello dell'altro utente deve restare intatto.
