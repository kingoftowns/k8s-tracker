terraform {
  required_providers {
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.24"
    }
  }
}

provider "kubernetes" {
  config_path    = "~/.kube/config"
}

locals {
    API_ENDPOINT = "https://cluster-info.k8s.blacktoaster.com"
    app_name = "cluster-info-collector"
    container_image = "registry.k8s.blacktoaster.com/cluster-info-collector:latest"
    namespace = "kube-system"
}

# Service Account
resource "kubernetes_service_account" "collector" {
  metadata {
    name      = local.app_name
    namespace = local.namespace
  }
}

# Cluster Role
resource "kubernetes_cluster_role" "collector" {
  metadata {
    name = local.app_name
  }

  rule {
    api_groups = [""]
    resources  = ["services"]
    verbs      = ["get", "list", "watch"]
  }

  rule {
    api_groups = ["networking.k8s.io"]
    resources  = ["ingresses"]
    verbs      = ["get", "list", "watch"]
  }

  rule {
    api_groups = [""]
    resources  = ["nodes"]
    verbs      = ["get", "list"]
  }

  rule {
    api_groups     = [""]
    resources      = ["configmaps"]
    resource_names = ["cluster-identity"]
    verbs          = ["get"]
  }
}

# Cluster Role Binding
resource "kubernetes_cluster_role_binding" "collector" {
  metadata {
    name = local.app_name
  }

  role_ref {
    api_group = "rbac.authorization.k8s.io"
    kind      = "ClusterRole"
    name      = kubernetes_cluster_role.collector.metadata[0].name
  }

  subject {
    kind      = "ServiceAccount"
    name      = kubernetes_service_account.collector.metadata[0].name
    namespace = local.namespace
  }
}

# CronJob
resource "kubernetes_cron_job_v1" "collector" {
  metadata {
    name      = local.app_name
    namespace = local.namespace
  }

  spec {
    schedule                      = "0 */6 * * *"
    concurrency_policy           = "Forbid"
    failed_jobs_history_limit    = 1
    successful_jobs_history_limit = 1

    job_template {
      metadata {
        name = local.app_name
      }

      spec {
        template {
          metadata {
            name = local.app_name
          }

          spec {
            service_account_name = kubernetes_service_account.collector.metadata[0].name
            
            container {
              name  = "info-collector"
              image = local.container_image

              env {
                name  = "API_ENDPOINT"
                value = local.API_ENDPOINT
              }
            }

            restart_policy = "OnFailure"
          }
        }
      }
    }
  }
}