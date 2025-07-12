# Migration Guide: Terraform to ArgoCD + Helm

## What We've Built

We've created a complete GitOps structure that uses ArgoCD with Helm charts to replace your Terraform deployment. The structure is designed to make it easy to eventually move each application to its own repository.

## Directory Structure Created

```
argocd/
├── README.md                    # Overview of the GitOps repository
├── MIGRATION_GUIDE.md          # This file
├── applications/               # ArgoCD Application definitions
│   ├── app-of-apps.yaml       # Root application (manages all others)
│   ├── database.yaml          # PostgreSQL database application
│   ├── backend.yaml           # Backend API (.NET) application
│   ├── frontend.yaml          # Frontend React application
│   └── controller.yaml        # Controller Go application
├── environments/              # Environment-specific Helm values
│   ├── pi4/                   # Pi4 cluster configuration
│   │   ├── database-values.yaml
│   │   ├── backend-values.yaml
│   │   ├── frontend-values.yaml
│   │   └── controller-values.yaml
│   └── pi5/                   # Pi5 cluster configuration
│       ├── database-values.yaml
│       ├── backend-values.yaml
│       ├── frontend-values.yaml
│       └── controller-values.yaml
└── base/                      # Helm charts (local now, remote later)
    ├── database/              # PostgreSQL chart
    │   ├── Chart.yaml
    │   ├── values.yaml
    │   └── templates/
    │       ├── namespace.yaml
    │       ├── secret.yaml
    │       ├── pv.yaml
    │       ├── pvc.yaml
    │       ├── statefulset.yaml
    │       ├── service.yaml
    │       └── _helpers.tpl
    ├── backend/               # Backend API chart
    │   ├── Chart.yaml
    │   ├── values.yaml
    │   └── templates/
    │       ├── deployment.yaml
    │       ├── service.yaml
    │       ├── certificate.yaml
    │       ├── ingress.yaml
    │       └── _helpers.tpl
    ├── frontend/              # Frontend chart (placeholder)
    │   ├── Chart.yaml
    │   └── values.yaml
    └── controller/            # Controller chart (placeholder)
        ├── Chart.yaml
        └── values.yaml
```

## What's Been Converted

### ✅ Completed
- **Directory Structure**: Complete GitOps layout
- **ArgoCD Applications**: App-of-apps pattern implemented
- **Database Chart**: Full PostgreSQL Helm chart with all templates
- **Backend Chart**: Complete .NET API chart with all resources
- **Frontend Chart**: Complete React frontend chart with all templates
- **Controller Chart**: Complete Go controller chart with all templates
- **Environment Values**: Pi4 and Pi5 specific configurations
- **Certificate Management**: cert-manager integration
- **Ingress Configuration**: NGINX ingress with TLS
- **RBAC**: Service accounts and permissions (controller)

### 🔄 TODO
- **Secret Management**: Using plain text passwords (needs proper secrets)
- **Repository URLs**: Using placeholder URLs

## Next Steps

### 1. **✅ All Templates Complete**
All Helm templates have been created:

```bash
# Frontend templates ✅ COMPLETE:
argocd/base/frontend/templates/
├── deployment.yaml
├── service.yaml
├── ingress.yaml
├── certificate.yaml
└── _helpers.tpl

# Controller templates ✅ COMPLETE:
argocd/base/controller/templates/
├── deployment.yaml
├── serviceaccount.yaml
├── clusterrole.yaml
├── clusterrolebinding.yaml
├── configmap.yaml
└── _helpers.tpl
```

### 2. **Setup Secret Management**
Replace plain text passwords with proper secret management:

```yaml
# Option 1: External Secrets Operator
apiVersion: external-secrets.io/v1beta1
kind: ExternalSecret

# Option 2: Sealed Secrets
apiVersion: bitnami.com/v1alpha1
kind: SealedSecret

# Option 3: ArgoCD Vault Plugin
```

### 3. **Update Repository URLs**
Replace placeholder URLs in all application files:
- `https://github.com/your-org/k8s-tracker-gitops`
- Update maintainer information

### 4. **Create the GitOps Repository**
```bash
# Create new repository
git init k8s-tracker-gitops
cd k8s-tracker-gitops

# Copy the argocd/ directory contents
cp -r /path/to/current/argocd/* .

# Initial commit
git add .
git commit -m "Initial GitOps setup with ArgoCD and Helm"
git remote add origin https://github.com/your-org/k8s-tracker-gitops.git
git push -u origin main
```

### 5. **Install ArgoCD**
```bash
# Create argocd namespace
kubectl create namespace argocd

# Install ArgoCD
kubectl apply -n argocd -f https://raw.githubusercontent.com/argoproj/argo-cd/stable/manifests/install.yaml

# Access ArgoCD UI
kubectl port-forward svc/argocd-server -n argocd 8080:443
```

### 6. **Deploy the App-of-Apps**
```bash
# Deploy the root application
kubectl apply -f applications/app-of-apps.yaml

# Check status
kubectl get applications -n argocd
```

### 7. **Environment-Specific Deployment**
Update ArgoCD applications to point to the correct environment:

```yaml
# For Pi4 cluster
helm:
  valueFiles:
    - ../../environments/pi4/database-values.yaml

# For Pi5 cluster  
helm:
  valueFiles:
    - ../../environments/pi5/database-values.yaml
```

## Future: Separate Repositories

When you're ready to move applications to separate repositories:

### 1. **Create Application Repositories**
```bash
# Create separate repos
k8s-tracker-backend/
├── src/                 # Your current Backend/ code
├── chart/              # Move argocd/base/backend/ here
│   ├── Chart.yaml
│   ├── values.yaml
│   └── templates/
└── Dockerfile
```

### 2. **Update ArgoCD Applications**
```yaml
# Change from local chart
source:
  repoURL: https://github.com/your-org/k8s-tracker-gitops
  path: argocd/base/backend

# To remote chart
source:
  repoURL: https://github.com/your-org/k8s-tracker-backend
  path: chart
```

### 3. **Keep Environment Values Centralized**
The `environments/` directory stays in the GitOps repo for centralized configuration management.

## Benefits of This Structure

1. **GitOps**: All changes through Git
2. **Environment Management**: Easy pi4/pi5 differences
3. **Scalable**: Ready for repository separation
4. **Consistent**: Same deployment pattern across all apps
5. **Rollback**: Easy to rollback deployments
6. **Audit**: Complete change history

## Testing the Migration

1. **Validate Helm Charts**: `helm template` each chart
2. **Check ArgoCD Apps**: Verify application definitions
3. **Test Environment Differences**: Ensure pi4/pi5 values work
4. **Dry Run**: Use ArgoCD sync with `--dry-run`

## Rollback Plan

Keep your Terraform configuration until the ArgoCD deployment is fully validated and working in production. 