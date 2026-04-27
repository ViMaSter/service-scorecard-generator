# ServiceMaturity

## About
This check reports customer n8n workload cluster maturity.

Repositories only qualify if containing a configuration for a workload cluster.

Maturity levels:

- L1: Legacy operator/controller style n8n (`n8n` custom resource under `managed-services-config`)
- L2: Manual n8n deployment (plain deployment/manual chart wiring)
- L3: managed-n8n Helm chart deployment


## How detection works
The check inspects known GitOps and bootstrap paths in each cluster repository and returns the highest matching level.

## Why this check exists
The operations team provides services before the platform team has completed building an abstraction to onboard and manage customers.  
As new procedures develop, the operations team requires a way to track the state of customers' workload clusters.
