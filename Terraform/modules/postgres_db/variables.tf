variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "container_image" {
  description = "The container image to use"
  type        = string
}

variable "db_name" {
  description = "The name of the database"
  type        = string
}

variable "db_port" {
  description = "The port for the database"
  type        = number
  default     = 5432
}

variable "db_username" {
  description = "The username for the database"
  type        = string
}

variable "namespace" {
  description = "The namespace to deploy the application"
  type        = string
}

variable "nfs_server" {
  description = "The NFS server"
  type        = string
}

variable "nfs_path" {
  description = "The NFS path"
  type        = string
}

variable "storage_size" {
  description = "The size of the storage"
  type        = string
  default     = "5Gi"
}