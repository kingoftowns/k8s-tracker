terraform {
  required_providers {
    kubernetes = {
      source = "hashicorp/kubernetes"
    }
  }
}

provider "kubernetes" {
  config_path = "~/.kube/config"
}

locals {
  api_endpoint = "https://cluster-info.k8s.blacktoaster.com"
  app_name = "k8s-watcher"
  configmap_name = "cluster-identity"
  container_image = "registry.k8s.blacktoaster.com/k8s-watcher:latest"
  namespace = "kube-system"
  service_account_name = "cluster-info-collector"
  ca_cert_configmap_name = "bt-ca"
}

# ConfigMap for the CA certificate
resource "kubernetes_config_map" "ca_cert" {
  metadata {
    name = local.ca_cert_configmap_name
    namespace = local.namespace
  }

  data = {
    "ca.crt" = file("./ca.crt")  # Make sure to have your CA certificate in this path
  }
}

resource "kubernetes_deployment" "watcher" {
  metadata {
    name = local.app_name
    namespace = local.namespace
    labels = {
      app = local.app_name
    }
  }

  spec {
    replicas = 1
    selector {
      match_labels = {
        app = local.app_name
      }
    }
    template {
      metadata {
        labels = {
          app = local.app_name
        }
      }
      spec {
        service_account_name = local.service_account_name
        container {
          name = "watcher"
          image = local.container_image

          env {
            name = "API_ENDPOINT"
            value = local.api_endpoint
          }
          env {
            name = "CONFIGMAP_NAME"
            value = local.configmap_name
          }
          env {
            name = "CONFIGMAP_NAMESPACE"
            value = local.namespace
          }
          env {
            name = "CA_CERT_PATH"
            value = "/etc/ssl/certs/ca.crt"
          }

          volume_mount {
            name = "ca-cert"
            mount_path = "/etc/ssl/certs"
            read_only = true
          }

          resources {
            limits = {
              cpu = "200m"
              memory = "256Mi"
            }
            requests = {
              cpu = "100m"
              memory = "128Mi"
            }
          }

          liveness_probe {
            exec {
              command = ["pidof", "watcher"]
            }
            initial_delay_seconds = 15
            period_seconds = 20
          }

          readiness_probe {
            exec {
              command = ["pidof", "watcher"]
            }
            initial_delay_seconds = 5
            period_seconds = 10
          }
        }

        volume {
          name = "ca-cert"
          config_map {
            name = local.ca_cert_configmap_name
            items {
              key = "ca.crt"
              path = "ca.crt"
            }
          }
        }
      }
    }
  }
}