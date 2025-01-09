output "container_image" {
  value = var.container_image
}

output "postgres_db_name" {
  value = var.db_name
}

output "postgres_db_password" {
  value     = random_string.postgres_password.result
  sensitive = true
}