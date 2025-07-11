variable "app_name" {
  description = "The name of the application"
  type        = string
  default     = "k8s-tracker"
}

variable "cluster_name" {
  description = "The name of the cluster"
  type        = string
}

variable "db_username" {
  description = "The username for the database"
  type        = string
  default     = "k8sTracker"
}

variable "domain" {
  description = "The domain to deploy the application"
  type        = string
  default     = "k8s.blacktoaster.com"
}

variable "namespace" {
  description = "The namespace to deploy the application"
  type        = string
  default     = "cluster-tracker"
}

variable "nfs_server" {
  description = "The NFS server"
  type        = string
  default     = "nfs.blacktoaster.com"
}

variable "nfs_path" {
  description = "The NFS path"
  type        = string
  default     = "/k8s"
}

variable "state_bucket" {
  description = "S3 bucket for terraform state"
  type        = string
}

variable "state_region" {
  description = "AWS region for S3 state bucket"
  type        = string
}

variable "state_cluster_prefix" {
  description = "Cluster prefix for state file naming"
  type        = string
}