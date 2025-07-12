# K8s Tracker GitOps Repository

This repository contains the GitOps configuration for the Kubernetes Tracker application using ArgoCD and Helm.

## Directory Structure

```
argocd/
├── applications/           # ArgoCD Application definitions
│   ├── app-of-apps.yaml   # Root application managing all others
│   ├── database.yaml      # Database application
│   ├── backend.yaml       # Backend API application
│   ├── frontend.yaml      # Frontend application
│   └── controller.yaml    # Controller application
├── environments/          # Environment-specific Helm values
│   ├── pi4/              # Pi4 cluster configuration
│   │   ├── database-values.yaml
│   │   ├── backend-values.yaml
│   │   ├── frontend-values.yaml
│   │   └── controller-values.yaml
│   └── pi5/              # Pi5 cluster configuration
│       ├── database-values.yaml
│       ├── backend-values.yaml
│       ├── frontend-values.yaml
│       └── controller-values.yaml
└── base/                  # Helm charts (local now, remote when code is split)
    ├── database/         # PostgreSQL database chart
    ├── backend/          # .NET Backend API chart
    ├── frontend/         # React Frontend chart
    └── controller/       # Go Controller chart
```

## Migration Status

- [ ] Current: Terraform-based deployment
- [ ] Target: ArgoCD + Helm GitOps deployment
- [ ] Future: Separate repositories for each component

## Usage

1. Deploy ArgoCD to your cluster
2. Apply the app-of-apps application: `kubectl apply -f applications/app-of-apps.yaml`
3. ArgoCD will automatically sync all applications

## Environment Management

Each environment (pi4, pi5) has its own values files that override the default chart values. 