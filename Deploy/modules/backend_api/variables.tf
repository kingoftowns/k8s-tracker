variable "app_name" {
  description = "The name of the application"
  type        = string
}

variable "cname_target" {
  description = "The target for the CNAME record"
  type        = string
}

variable "container_image" {
  description = "The container image to use"
  type        = string
}

variable "db_image" {
  description = "The database image to use"
  type        = string
}

variable "db_name" {
  description = "The name of the database"
  type        = string
}

variable "db_username" {
  description = "The username for the database"
  type        = string
}

variable "db_password" {
  description = "The password for the database"
  type        = string
}

variable "domain" {
  description = "The domain to deploy the application"
  type        = string
}

variable "namespace" {
  description = "The namespace to deploy the application"
  type        = string
}