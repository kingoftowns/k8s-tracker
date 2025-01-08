# k8s-tracker
backend for kubernetes cluster info


## TODO

currently you need to deploy the db first, then connect and run the migrations

from your host export the db port

```bash
kubectl port-forward sts/postgres 5432:5432
```

this makes the pg db avaiable from localhost, so grab the password generated when you deployed the db and update appsettings.json connection string, and then you can run

```bash
dotnet ef database update
```

once that completes, then you can deploy the api, need to make this work all the way through without that manual step 

## note if you ever re-deploy this app/db make sure you delete the content on whatever is backing the pv