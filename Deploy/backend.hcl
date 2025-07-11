# Shared backend configuration for all clusters
bucket = "k8s-bt-cluster-state"
region = "us-west-1"
encrypt = true
use_lockfile = true
# key will be specified dynamically per module and cluster