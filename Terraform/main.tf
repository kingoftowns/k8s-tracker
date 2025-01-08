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
      storage = "5Gi"
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
        storage = "5Gi"
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
        container {
          name  = "postgres"
          image = "postgres:14-alpine"

          port {
            container_port = 5432
          }

          env {
            name  = "POSTGRES_DB"
            value = "kubernetes_tracking"
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
    name = "postgres"
    namespace = var.namespace
  }

  spec {
    selector = {
      app = "postgres"
    }

    port {
      port        = 5432
      target_port = 5432
    }
  }
}

resource "kubernetes_deployment" "api" {
  metadata {
    name      = "${var.app_name}-api"
    namespace = var.namespace
  }

  spec {
    replicas = 1

    selector {
      match_labels = {
        app = "${var.app_name}-api"
      }
    }

    template {
      metadata {
        labels = {
          app = "${var.app_name}-api"
        }
      }

      spec {
        container {
          name              = "api"
          image             = "registry.k8s.blacktoaster.com/k8s-tracker/k8s-tracker-api:latest"
          image_pull_policy = "Always"

          port {
            container_port = 80
          }

          env {
            name  = "ConnectionStrings__DefaultConnection"
            value = "Host=postgres;Database=kubernetes_tracking;Username=${var.db_username};Password=${random_string.postgres_password.result}"
          }
        }
      }
    }
  }
}

resource "kubernetes_service" "api" {
  metadata {
    name      = "${var.app_name}-svc"
    namespace = var.namespace
  }

  spec {
    type = "ClusterIP"

    selector = {
      app = "${var.app_name}-api"
    }

    port {
      protocol    = "TCP"
      port        = 80
      target_port = 80
    }
  }
}

resource "kubectl_manifest" "certificate" {
  yaml_body = yamlencode({
    apiVersion = "cert-manager.io/v1"
    kind       = "Certificate"
    metadata = {
      name      = var.app_name
      namespace = kubernetes_namespace.tracking.metadata[0].name
    }
    spec = {
      commonName = "cluster-info.${var.domain}"
      secretName = "${var.app_name}-tls"
      issuerRef = {
        name  = "vault-issuer"
        kind  = "ClusterIssuer"
        group = "cert-manager.io"
      }
      dnsNames = [
        "cluster-info.${var.domain}"
      ]
    }
  })
}

resource "kubernetes_ingress_v1" "k8sTracker" {
  metadata {
    name      = "${var.app_name}-ingress"
    namespace = kubernetes_namespace.tracking.metadata[0].name
    annotations = {
      "nginx.ingress.kubernetes.io/ssl-redirect"     = "true"
      "nginx.ingress.kubernetes.io/backend-protocol" = "HTTP"
      "external-dns.alpha.kubernetes.io/target"      = "in.k8s.blacktoaster.com"
    }
  }

  spec {
    ingress_class_name = "nginx"
    tls {
      hosts       = ["cluster-info.${var.domain}"]
      secret_name = "${var.app_name}-tls"
    }
    rule {
      host = "cluster-info.${var.domain}"
      http {
        path {
          path      = "/"
          path_type = "Prefix"
          backend {
            service {
              name = kubernetes_service.api.metadata[0].name
              port {
                number = 80
              }
            }
          }
        }
      }
    }
  }

  depends_on = [kubectl_manifest.certificate]
}