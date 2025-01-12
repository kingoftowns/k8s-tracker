from kubernetes import client, config
import logging
import requests
import os
import sys

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    stream=sys.stdout
)

logger = logging.getLogger(__name__)

def get_cluster_info():
    try:
        config.load_incluster_config()
        logger.info("Using in-cluster configuration")
    except config.ConfigException:
        config.load_kube_config()
        logger.info("Using local configuration")

    v1 = client.CoreV1Api()
    version_api = client.VersionApi()

    version_info = version_api.get_code()
    api_version = version_info.git_version

    kernel_versions = set()
    kubelet_versions = set()

    nodes = v1.list_node()
    for node in nodes.items:
        kernel_versions.add(node.status.node_info.kernel_version)
        kubelet_versions.add(node.status.node_info.kubelet_version)

    try:
        config_map = v1.read_namespaced_config_map('cluster-identity', 'kube-system')
        cluster_name = config_map.data.get('cluster-name', 'unknown')
    except client.exceptions.ApiException as e:
        logger.error(f"Error reading cluster-identity configmap: {e}")
        sys.exit(1)

    cluster_info = {
        'clusterName': cluster_name,
        'apiServerVersion': api_version,
        'kubeletVersions': list(kubelet_versions),
        'kernelVersions': list(kernel_versions)
    }

    return cluster_info

def send_to_api(cluster_info):
    api_endpoint = os.getenv('API_ENDPOINT')
    
    if not api_endpoint:
        raise("No API_ENDPOINT specified")
        
    
    headers = {
        'Content-Type': 'application/json',
    }

    try:
        all_clusters = requests.get(
            f"{api_endpoint}/api/clusters",
            headers=headers,
            verify='ca.crt'
        ).json()
    except requests.exceptions.RequestException as e:
        logger.error(f"Error reading data from API: {e}")
        sys.exit(1)
    
    found_cluster = [cluster['id'] for cluster in all_clusters if cluster['clusterName'] == cluster_info['clusterName']]
    
    if not found_cluster:
        logger.info("Cluster not found in db, creating new entry")
        try:
            response = requests.post(
                f"{api_endpoint}/api/clusters",
                json=cluster_info,
                headers=headers,
                verify='ca.crt'
            )
            response.raise_for_status()
            logger.info(f"cluster added to db: {cluster_info}")
            logger.info(f"Successfully sent data to API: {response.status_code}")
        except requests.exceptions.RequestException as e:
            logger.error(f"Error posting to API: {e}")
            sys.exit(1)
    else:
        found_cluster = found_cluster[0]
        try:
            logger.info(f"cluster exists in database with id: {found_cluster}")
            response = requests.put(
                f"{api_endpoint}/api/clusters/{found_cluster}",
                json=cluster_info,
                headers=headers,
                verify='ca.crt'
            )
            response.raise_for_status()
            logger.info(f"Successfully updated cluster id: {found_cluster} in db")
            logger.info(f"Successfully sent data: {cluster_info}")
        except requests.exceptions.RequestException as e:
            logger.error(f"Error updating db: {e}")
            sys.exit(1)
        

def main():
    cluster_info = get_cluster_info()
    send_to_api(cluster_info)

if __name__ == "__main__":
    main()
