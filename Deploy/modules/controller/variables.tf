variable "api_endpoint" {
  description = "The API endpoint"
  type        = string
}

variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "ca_cert_configmap_name" {
  description = "The name of the CA certificate configmap"
  type        = string
}

variable "cluster_name" {
  description = "The name of the cluster"
  type        = string
}

variable "configmap_name" {
  description = "The name of the configmap"
  type        = string
}

variable "controller_image" {
  description = "The container image to use"
  type        = string
}

variable "namespace" {
  description = "The namespace to deploy the application"
  type        = string
}

variable "service_account_name" {
  description = "The name of the service account"
  type        = string
}