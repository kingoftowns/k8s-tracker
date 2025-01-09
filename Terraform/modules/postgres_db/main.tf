resource "kubernetes_namespace" "tracking" {
  metadata {
    name = var.namespace
  }
}

resource "random_string" "postgres_password" {
  length  = 32
  special = false
}

resource "kubernetes_persistent_volume" "postgres_pv" {
  metadata {
    name = "${var.app_name}-nfs-pv"
  }
  spec {
    capacity = {
      storage = var.storage_size
    }
    access_modes = ["ReadWriteMany"]
    persistent_volume_source {
      nfs {
        server = var.nfs_server
        path   = "${var.nfs_path}/${var.app_name}"
      }
    }
    storage_class_name = "nfs-storage"
  }
}

resource "kubernetes_persistent_volume_claim" "postgres_pvc" {
  metadata {
    name      = "${var.app_name}-pvc"
    namespace = var.namespace
    labels = {
      "app.kubernetes.io/name"     = var.app_name
      "app.kubernetes.io/instance" = var.app_name
    }
  }
  spec {
    access_modes       = ["ReadWriteMany"]
    storage_class_name = "nfs-storage"
    resources {
      requests = {
        storage = var.storage_size
      }
    }
  }

  depends_on = [kubernetes_persistent_volume.postgres_pv]
}

resource "kubernetes_secret" "db_credentials" {
  metadata {
    name      = "postgres-secret"
    namespace = var.namespace
  }

  data = {
    username = var.db_username
    password = random_string.postgres_password.result
  }
}

resource "kubernetes_stateful_set" "postgres" {
  metadata {
    name      = "postgres"
    namespace = var.namespace
  }

  spec {
    service_name = "postgres"
    replicas     = 1

    selector {
      match_labels = {
        app = "postgres"
      }
    }

    template {
      metadata {
        labels = {
          app = "postgres"
        }
      }

      spec {
        
        node_selector = {
          hardware = "pi5"
        }

        toleration {
          key      = "hardware"
          operator = "Equal"
          value    = "pi5"
          effect   = "NoSchedule"
        }

        container {
          name  = "postgres"
          image = var.container_image

          port {
            container_port = var.db_port
          }

          env {
            name  = "POSTGRES_DB"
            value = var.db_name
          }
          env {
            name = "POSTGRES_USER"
            value_from {
              secret_key_ref {
                name = kubernetes_secret.db_credentials.metadata[0].name
                key  = "username"
              }
            }
          }
          env {
            name = "POSTGRES_PASSWORD"
            value_from {
              secret_key_ref {
                name = kubernetes_secret.db_credentials.metadata[0].name
                key  = "password"
              }
            }
          }

          volume_mount {
            name       = "postgres-storage"
            mount_path = "/var/lib/postgresql/data"
          }
        }

        volume {
          name = "postgres-storage"
          persistent_volume_claim {
            claim_name = kubernetes_persistent_volume_claim.postgres_pvc.metadata[0].name
          }
        }
      }
    }
  }
}

resource "kubernetes_service" "postgres" {
  metadata {
    name      = "postgres"
    namespace = var.namespace
  }

  spec {
    selector = {
      app = "postgres"
    }

    port {
      port        = var.db_port
      target_port = var.db_port
    }
  }
}