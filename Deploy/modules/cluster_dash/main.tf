terraform {
  required_providers {
    kubectl = {
      source = "gavinbunney/kubectl"
    }
  }
}


resource "kubernetes_deployment" "api" {
  metadata {
    name      = var.app_name
    namespace = var.namespace
  }

  spec {
    replicas = 1

    selector {
      match_labels = {
        app = "${var.app_name}"
      }
    }

    template {
      metadata {
        labels = {
          app = "${var.app_name}"
        }
      }

      spec {
        container {
          name              = var.app_name
          image             = var.container_image
          image_pull_policy = "Always"

          port {
            container_port = 80
          }

          env {
            name  = "REACT_APP_CLUSTER_API_UR"
            value = var.tracker_api_url
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
      app = "${var.app_name}"
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
      namespace = var.namespace
    }
    spec = {
      commonName = "clusters.${var.domain}"
      secretName = "${var.app_name}-tls"
      issuerRef = {
        name  = "vault-issuer"
        kind  = "ClusterIssuer"
        group = "cert-manager.io"
      }
      dnsNames = [
        "clusters.${var.domain}"
      ]
    }
  })
}

resource "kubernetes_ingress_v1" "k8sTracker" {
  metadata {
    name      = "${var.app_name}-ingress"
    namespace = var.namespace
    annotations = {
      "nginx.ingress.kubernetes.io/ssl-redirect"     = "true"
      "nginx.ingress.kubernetes.io/backend-protocol" = "HTTP"
      "external-dns.alpha.kubernetes.io/target"      = var.cname_target
    }
  }

  spec {
    ingress_class_name = "nginx"
    tls {
      hosts       = ["clusters.${var.domain}"]
      secret_name = "${var.app_name}-tls"
    }
    rule {
      host = "clusters.${var.domain}"
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