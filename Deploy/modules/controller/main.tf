resource "kubernetes_service_account" "collector" {
  metadata {
    name      = var.service_account_name
    namespace = var.namespace
  }
}

resource "kubernetes_cluster_role" "collector" {
  metadata {
    name = var.app_name
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

resource "kubernetes_cluster_role_binding" "collector" {
  metadata {
    name = var.app_name
  }

  role_ref {
    api_group = "rbac.authorization.k8s.io"
    kind      = "ClusterRole"
    name      = kubernetes_cluster_role.collector.metadata[0].name
  }

  subject {
    kind      = "ServiceAccount"
    name      = kubernetes_service_account.collector.metadata[0].name
    namespace = var.namespace
  }
}

resource "kubernetes_config_map" "cluster-identity" {
  metadata {
    name      = var.configmap_name
    namespace = var.namespace
  }

  data = {
    cluster-environment = "production"
    cluster_name = "bt-pi-cluster"
  }
}

resource "kubernetes_deployment" "watcher" {
  metadata {
    name      = var.app_name
    namespace = var.namespace
    labels = {
      app = var.app_name
    }
  }

  spec {
    replicas = 1
    selector {
      match_labels = {
        app = var.app_name
      }
    }
    template {
      metadata {
        labels = {
          app = var.app_name
        }
      }
      spec {
        service_account_name = var.service_account_name
        container {
          name  = "watcher"
          image = var.controller_image

          env {
            name  = "API_ENDPOINT"
            value = var.api_endpoint
          }
          env {
            name  = "CONFIGMAP_NAME"
            value = var.configmap_name
          }
          env {
            name  = "CONFIGMAP_NAMESPACE"
            value = var.namespace
          }

          resources {
            limits = {
              cpu    = "200m"
              memory = "256Mi"
            }
            requests = {
              cpu    = "100m"
              memory = "128Mi"
            }
          }

          liveness_probe {
            exec {
              command = ["pidof", "watcher"]
            }
            initial_delay_seconds = 15
            period_seconds        = 20
          }

          readiness_probe {
            exec {
              command = ["pidof", "watcher"]
            }
            initial_delay_seconds = 5
            period_seconds        = 10
          }
        }
      }
    }
  }
}