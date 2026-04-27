# ServiceMaturity

## About
This check reports customer n8n workload maturity for infrastructure cluster repositories.

Maturity levels:
- L0: No customer n8n workload detected
- L1: Legacy operator/controller style n8n (N8n custom resource under managed-services-config)
- L2: Manual n8n deployment (plain deployment/manual chart wiring)
- L3: managed-n8n Helm chart deployment

## How detection works
The check inspects known GitOps and bootstrap paths in each cluster repository and returns the highest matching level.

## Why this check exists
This makes rollout status transparent across clusters and helps prioritize migration from legacy or manual setups to managed n8n deployment patterns.
