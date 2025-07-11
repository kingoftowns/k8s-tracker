module "ks8_tracker_db" {
  source = "./modules/postgres_db"

  app_name        = var.app_name
  container_image = "postgres:14-alpine"
  db_name         = "kubernetes_tracking"
  db_username     = var.db_username
  namespace       = var.namespace
  nfs_path        = var.nfs_path
  nfs_server      = var.nfs_server
}

module "ks8_tracker_api" {
  source = "./modules/backend_api"

  app_name        = var.app_name
  cname_target    = "in.k8s.blacktoaster.com"
  container_image = "registry.k8s.blacktoaster.com/k8s-tracker/k8s-tracker-api:latest"
  db_image        = module.ks8_tracker_db.container_image
  db_name         = module.ks8_tracker_db.postgres_db_name
  db_password     = module.ks8_tracker_db.postgres_db_password
  db_username     = var.db_username
  domain          = var.domain
  namespace       = var.namespace

  depends_on = [module.ks8_tracker_db]
}

module "ks8_tracker_ui" {
  source = "./modules/cluster_dash"

  app_name        = "k8s-dashboard"
  cname_target    = "in.k8s.blacktoaster.com"
  container_image = "registry.k8s.blacktoaster.com/k8s-tracker/k8s-dashboard:latest"
  domain          = var.domain
  namespace       = var.namespace
  tracker_api_url = "https://cluster-info.k8s.blacktoaster.com"
}

module "k8s_controller" {
  source = "./modules/controller"

  api_endpoint           = "https://cluster-info.k8s.blacktoaster.com"
  app_name               = "k8s-controller"
  ca_cert_configmap_name = "bt-ca"
  cluster_name           = var.cluster_name
  configmap_name         = "cluster-identity"
  controller_image       = "registry.k8s.blacktoaster.com/k8s-tracker/k8s-controller:latest"
  namespace              = var.namespace
  service_account_name   = "cluster-info-collector"
}