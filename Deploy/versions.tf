terraform {
  required_version = "~> 1.9"

  required_providers {
    kubectl = {
      source  = "gavinbunney/kubectl"
      version = ">= 1.16.0"
    }

    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = ">= 2.34.0"
    }

    random = {
      source  = "hashicorp/random"
      version = "~> 3.5"
    }
  }

  backend "s3" {
    # Backend configuration will be provided via backend.hcl and command line
  }
}

provider "kubectl" {
  config_path = "~/.kube/config"
}

provider "kubernetes" {
  config_path = "~/.kube/config"
}