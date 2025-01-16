package main

import (
	"bytes"
	"context"
	"crypto/tls"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"net/http"
	"os"
	"sort"
	"time"

	corev1 "k8s.io/api/core/v1"
	networkingv1 "k8s.io/api/networking/v1"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/apimachinery/pkg/fields"
	"k8s.io/client-go/kubernetes"
	"k8s.io/client-go/rest"
	"k8s.io/client-go/tools/cache"
)

type Config struct {
	APIEndpoint        string
	ConfigMapName      string
	ConfigMapNamespace string
	CACertPath         string
}

// LoadConfig loads configuration from environment variables
func LoadConfig() (*Config, error) {
	apiEndpoint := os.Getenv("API_ENDPOINT")
	if apiEndpoint == "" {
		return nil, fmt.Errorf("API_ENDPOINT environment variable is required")
	}
	log.Printf("Loaded API_ENDPOINT: %s", apiEndpoint)

	configMapName := os.Getenv("CONFIGMAP_NAME")
	if configMapName == "" {
		return nil, fmt.Errorf("CONFIGMAP_NAME environment variable is required")
	}
	log.Printf("Loaded CONFIGMAP_NAME: %s", configMapName)

	configMapNamespace := os.Getenv("CONFIGMAP_NAMESPACE")
	if configMapNamespace == "" {
		return nil, fmt.Errorf("CONFIGMAP_NAMESPACE environment variable is required")
	}
	log.Printf("Loaded CONFIGMAP_NAMESPACE: %s", configMapNamespace)

	caCertPath := os.Getenv("CA_CERT_PATH")
	if caCertPath == "" {
		caCertPath = "/etc/ssl/certs/ca.crt" // Default path
	}
	log.Printf("Using CA certificate from: %s", caCertPath)

	return &Config{
		APIEndpoint:        apiEndpoint,
		ConfigMapName:      configMapName,
		ConfigMapNamespace: configMapNamespace,
		CACertPath:         caCertPath,
	}, nil
}

type ResourceWatcher struct {
	clientset   *kubernetes.Clientset
	clusterName string
	httpClient  *http.Client
	config      *Config
}

type ClusterInfo struct {
	ClusterName      string   `json:"clusterName"`
	APIServerVersion string   `json:"apiServerVersion"`
	KubeletVersions  []string `json:"kubeletVersions"`
	KernelVersions   []string `json:"kernelVersions"`
}

type IngressPayload struct {
	ClusterName string   `json:"clusterName"`
	Namespace   string   `json:"namespace"`
	IngressName string   `json:"ingressName"`
	Hosts       []string `json:"hosts"`
	Ports       []int32  `json:"ports"`
}

type IngressResponse struct {
	ID          int      `json:"id"`
	ClusterName string   `json:"clusterName"`
	Namespace   string   `json:"namespace"`
	IngressName string   `json:"ingressName"`
	Hosts       []string `json:"hosts"`
	Ports       []int32  `json:"ports"`
}

type ServicePayload struct {
	ClusterName string  `json:"clusterName"`
	Namespace   string  `json:"namespace"`
	ServiceName string  `json:"serviceName"`
	ExternalIP  string  `json:"externalIp"`
	Ports       []int32 `json:"ports"`
	ServiceType string  `json:"serviceType"`
}

type ServiceResponse struct {
	ID          int     `json:"id"`
	ClusterName string  `json:"clusterName"`
	Namespace   string  `json:"namespace"`
	ServiceName string  `json:"serviceName"`
	ExternalIP  string  `json:"externalIp"`
	Ports       []int32 `json:"ports"`
	ServiceType string  `json:"serviceType"`
}

func NewResourceWatcher(k8sConfig *rest.Config, appConfig *Config) (*ResourceWatcher, error) {
	clientset, err := kubernetes.NewForConfig(k8sConfig)
	if err != nil {
		return nil, fmt.Errorf("failed to create kubernetes clientset: %v", err)
	}

	// Get cluster name from ConfigMap
	cm, err := clientset.CoreV1().ConfigMaps(appConfig.ConfigMapNamespace).Get(
		context.Background(),
		appConfig.ConfigMapName,
		metav1.GetOptions{},
	)
	if err != nil {
		return nil, fmt.Errorf("failed to get cluster-identity configmap: %v", err)
	}

	clusterName, ok := cm.Data["cluster-name"]
	if !ok {
		return nil, fmt.Errorf("cluster-name not found in configmap")
	}

	return &ResourceWatcher{
		clientset:   clientset,
		clusterName: clusterName,
		httpClient: &http.Client{
			Timeout: 10 * time.Second,
			Transport: &http.Transport{
				TLSClientConfig: &tls.Config{
					InsecureSkipVerify: true,
				},
			},
		},
		config: appConfig,
	}, nil
}

func (w *ResourceWatcher) WatchResources(ctx context.Context) error {
	// Run initial cluster info collection
	log.Printf("Performing initial cluster info collection...")
	if err := w.collectAndSendClusterInfo(); err != nil {
		return fmt.Errorf("initial cluster info collection failed: %v", err)
	}
	log.Printf("Initial cluster info collection completed successfully")

	go func() {
		ticker := time.NewTicker(4 * time.Hour)
		defer ticker.Stop()

		for {
			select {
			case <-ctx.Done():
				return
			case <-ticker.C:
				if err := w.collectAndSendClusterInfo(); err != nil {
					log.Printf("Periodic cluster info collection failed: %v", err)
				}
			}
		}
	}()

	// Watch Ingresses
	ingressListWatcher := cache.NewListWatchFromClient(
		w.clientset.NetworkingV1().RESTClient(),
		"ingresses",
		corev1.NamespaceAll,
		fields.Everything(),
	)

	_, ingressController := cache.NewInformer(
		ingressListWatcher,
		&networkingv1.Ingress{},
		time.Second*30,
		cache.ResourceEventHandlerFuncs{
			AddFunc: w.handleIngressChange,
			UpdateFunc: func(oldObj, newObj interface{}) {
				w.handleIngressChange(newObj)
			},
			DeleteFunc: w.handleIngressDelete,
		},
	)

	// Watch Services
	serviceListWatcher := cache.NewListWatchFromClient(
		w.clientset.CoreV1().RESTClient(),
		"services",
		corev1.NamespaceAll,
		fields.Everything(),
	)

	_, serviceController := cache.NewInformer(
		serviceListWatcher,
		&corev1.Service{},
		time.Second*30,
		cache.ResourceEventHandlerFuncs{
			AddFunc: w.handleServiceChange,
			UpdateFunc: func(oldObj, newObj interface{}) {
				w.handleServiceChange(newObj)
			},
			DeleteFunc: w.handleServiceDelete,
		},
	)

	// Start controllers
	go ingressController.Run(ctx.Done())
	go serviceController.Run(ctx.Done())

	// Wait for context cancellation
	<-ctx.Done()
	return nil
}

func (w *ResourceWatcher) collectClusterInfo() (*ClusterInfo, error) {
	serverVersion, err := w.clientset.Discovery().ServerVersion()
	if err != nil {
		return nil, fmt.Errorf("failed to get server version: %v", err)
	}

	nodes, err := w.clientset.CoreV1().Nodes().List(context.TODO(), metav1.ListOptions{})
	if err != nil {
		return nil, fmt.Errorf("failed to list nodes: %v", err)
	}

	kernelVersions := make(map[string]struct{})
	kubeletVersions := make(map[string]struct{})

	for _, node := range nodes.Items {
		kernelVersions[node.Status.NodeInfo.KernelVersion] = struct{}{}
		kubeletVersions[node.Status.NodeInfo.KubeletVersion] = struct{}{}
	}

	kernelVersionsList := make([]string, 0, len(kernelVersions))
	kubeletVersionsList := make([]string, 0, len(kubeletVersions))

	for version := range kernelVersions {
		kernelVersionsList = append(kernelVersionsList, version)
	}
	for version := range kubeletVersions {
		kubeletVersionsList = append(kubeletVersionsList, version)
	}

	return &ClusterInfo{
		ClusterName:      w.clusterName,
		APIServerVersion: serverVersion.GitVersion,
		KubeletVersions:  kubeletVersionsList,
		KernelVersions:   kernelVersionsList,
	}, nil
}

func (w *ResourceWatcher) sendClusterInfo(clusterInfo *ClusterInfo) error {
	resp, err := w.httpClient.Get(
		fmt.Sprintf("%s/api/clusters/name/%s", w.config.APIEndpoint, w.clusterName),
	)
	if err != nil {
		return fmt.Errorf("failed to check cluster existence: %v", err)
	}
	defer resp.Body.Close()

	type ClusterResponse struct {
		ID               int      `json:"id"`
		ClusterName      string   `json:"clusterName"`
		APIServerVersion string   `json:"apiserverVersion"`
		KubeletVersions  []string `json:"kubeletVersions"`
	}

	var req *http.Request
	jsonData, err := json.Marshal(clusterInfo)
	if err != nil {
		return fmt.Errorf("failed to marshal cluster info: %v", err)
	}

	if resp.StatusCode == http.StatusOK {
		// Cluster exists, get its ID and update
		var existingCluster ClusterResponse
		if err := json.NewDecoder(resp.Body).Decode(&existingCluster); err != nil {
			return fmt.Errorf("failed to decode existing cluster response: %v", err)
		}

		req, err = http.NewRequest(
			http.MethodPut,
			fmt.Sprintf("%s/api/clusters/%d", w.config.APIEndpoint, existingCluster.ID),
			bytes.NewBuffer(jsonData),
		)
		log.Printf("Updating existing cluster with ID: %d", existingCluster.ID)
	} else {
		// Cluster doesn't exist, create new
		req, err = http.NewRequest(
			http.MethodPost,
			fmt.Sprintf("%s/api/clusters", w.config.APIEndpoint),
			bytes.NewBuffer(jsonData),
		)
		log.Printf("Creating new cluster entry")
	}

	if err != nil {
		return fmt.Errorf("failed to create request: %v", err)
	}
	req.Header.Set("Content-Type", "application/json")

	resp, err = w.httpClient.Do(req)
	if err != nil {
		return fmt.Errorf("failed to send request: %v", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusCreated {
		body, _ := io.ReadAll(resp.Body)
		return fmt.Errorf("received non-OK response: %d - %s", resp.StatusCode, string(body))
	}

	return nil
}

func (w *ResourceWatcher) collectAndSendClusterInfo() error {
	clusterInfo, err := w.collectClusterInfo()
	if err != nil {
		return fmt.Errorf("failed to collect cluster info: %v", err)
	}

	if err := w.sendClusterInfo(clusterInfo); err != nil {
		return fmt.Errorf("failed to send cluster info: %v", err)
	}

	log.Printf("Successfully collected and sent cluster info for cluster: %s", w.clusterName)
	return nil
}

func (w *ResourceWatcher) createIngressPayload(ingress *networkingv1.Ingress) IngressPayload {
	if ingress == nil {
		return IngressPayload{
			ClusterName: w.clusterName,
			Hosts:       []string{},
			Ports:       []int32{},
		}
	}

	hosts := []string{}
	ports := []int32{}

	if ingress.Spec.Rules != nil {
		for _, rule := range ingress.Spec.Rules {
			if rule.Host != "" {
				hosts = append(hosts, rule.Host)
			}
			if rule.HTTP != nil {
				for _, path := range rule.HTTP.Paths {
					if path.Backend.Service != nil && path.Backend.Service.Port.Number > 0 {
						ports = append(ports, path.Backend.Service.Port.Number)
					}
				}
			}
		}
	}

	// Remove duplicate ports
	ports = uniquePorts(ports)

	return IngressPayload{
		ClusterName: w.clusterName,
		Namespace:   ingress.Namespace,
		IngressName: ingress.Name,
		Hosts:       hosts,
		Ports:       ports,
	}
}

func uniquePorts(ports []int32) []int32 {
	seen := make(map[int32]bool)
	unique := []int32{}

	for _, port := range ports {
		if !seen[port] {
			seen[port] = true
			unique = append(unique, port)
		}
	}

	// Sort ports for consistent output
	sort.Slice(unique, func(i, j int) bool {
		return unique[i] < unique[j]
	})

	return unique
}

func (w *ResourceWatcher) findIngressID(ingressName string) (int, error) {
	resp, err := w.httpClient.Get(
		fmt.Sprintf("%s/api/ingress/cluster/%s", w.config.APIEndpoint, w.clusterName),
	)
	if err != nil {
		return 0, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return 0, fmt.Errorf("unexpected status code: %d", resp.StatusCode)
	}

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return 0, err
	}

	var ingresses []IngressResponse
	if err := json.Unmarshal(body, &ingresses); err != nil {
		return 0, err
	}

	for _, ing := range ingresses {
		if ing.IngressName == ingressName {
			id := ing.ID
			return id, nil
		}
	}

	return 0, fmt.Errorf("ingress not found")
}

func (w *ResourceWatcher) handleIngressChange(obj interface{}) {
	if obj == nil {
		log.Printf("Error: received nil object in handleIngressChange")
		return
	}

	ingress, ok := obj.(*networkingv1.Ingress)
	if !ok {
		log.Printf("Error: unexpected type for ingress object")
		return
	}

	payload := w.createIngressPayload(ingress)
	if payload.Hosts == nil {
		payload.Hosts = []string{}
	}

	jsonData, err := json.Marshal(payload)
	if err != nil {
		log.Printf("Error marshaling payload: %v", err)
		return
	}

	// Try to find if the ingress already exists
	id, err := w.findIngressID(ingress.Name)
	var req *http.Request
	var actionType string

	if err == nil {
		// Ingress exists, do PUT
		req, err = http.NewRequest(
			http.MethodPut,
			fmt.Sprintf("%s/api/ingress/%d", w.config.APIEndpoint, id),
			bytes.NewBuffer(jsonData),
		)
		actionType = "UPDATE"
	} else {
		// Ingress doesn't exist, do POST
		req, err = http.NewRequest(
			http.MethodPost,
			fmt.Sprintf("%s/api/ingress", w.config.APIEndpoint),
			bytes.NewBuffer(jsonData),
		)
		actionType = "CREATE"
	}

	if err != nil {
		log.Printf("Error creating request: %v", err)
		return
	}
	req.Header.Set("Content-Type", "application/json")

	log.Printf("API %s Request - Ingress %s/%s - URL: %s", actionType, ingress.Namespace, ingress.Name, req.URL.String())

	resp, err := w.httpClient.Do(req)
	if err != nil {
		log.Printf("Error making HTTP request: %v", err)
		return
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusCreated {
		body, _ := io.ReadAll(resp.Body)
		log.Printf("API %s Response - Ingress %s/%s - Status: %d, Error: %s",
			actionType, ingress.Namespace, ingress.Name, resp.StatusCode, string(body))
	} else {
		log.Printf("API %s Response - Ingress %s/%s - Status: %d",
			actionType, ingress.Namespace, ingress.Name, resp.StatusCode)
	}
}

func (w *ResourceWatcher) handleIngressDelete(obj interface{}) {
	ingress := obj.(*networkingv1.Ingress)

	id, err := w.findIngressID(ingress.Name)
	if err != nil {
		log.Printf("Error finding ingress ID: %v", err)
		return
	}

	req, err := http.NewRequest(
		http.MethodDelete,
		fmt.Sprintf("%s/api/ingress/%d", w.config.APIEndpoint, id),
		nil,
	)
	if err != nil {
		log.Printf("Error creating request: %v", err)
		return
	}

	log.Printf("API DELETE Request - Ingress %s/%s - URL: %s", ingress.Namespace, ingress.Name, req.URL.String())

	resp, err := w.httpClient.Do(req)
	if err != nil {
		log.Printf("Error making DELETE request: %v", err)
		return
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusNoContent {
		body, _ := io.ReadAll(resp.Body)
		log.Printf("API DELETE Response - Ingress %s/%s - Status: %d, Error: %s",
			ingress.Namespace, ingress.Name, resp.StatusCode, string(body))
	} else {
		log.Printf("API DELETE Response - Ingress %s/%s - Status: %d",
			ingress.Namespace, ingress.Name, resp.StatusCode)
	}
}

func (w *ResourceWatcher) createServicePayload(service *corev1.Service) ServicePayload {
	if service == nil {
		return ServicePayload{
			ClusterName: w.clusterName,
			Ports:       []int32{},
		}
	}

	var externalIP string
	if len(service.Status.LoadBalancer.Ingress) > 0 {
		externalIP = service.Status.LoadBalancer.Ingress[0].IP
	}

	ports := []int32{}
	if service.Spec.Ports != nil {
		for _, port := range service.Spec.Ports {
			ports = append(ports, port.Port)
		}
	}

	serviceType := service.Spec.Type
	// or if Type is not set, it defaults to ClusterIP
	if serviceType == "" {
		serviceType = corev1.ServiceTypeClusterIP
		log.Printf("Setting default service type to ClusterIP for %s/%s", service.Namespace, service.Name)
	}

	return ServicePayload{
		ClusterName: w.clusterName,
		Namespace:   service.Namespace,
		ServiceName: service.Name,
		ExternalIP:  externalIP,
		Ports:       ports,
		ServiceType: string(serviceType),
	}
}

func (w *ResourceWatcher) findServiceID(serviceName string) (int, error) {
	resp, err := w.httpClient.Get(
		fmt.Sprintf("%s/api/service/cluster/%s", w.config.APIEndpoint, w.clusterName),
	)
	if err != nil {
		return 0, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return 0, fmt.Errorf("unexpected status code: %d", resp.StatusCode)
	}

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return 0, err
	}

	var services []ServiceResponse
	if err := json.Unmarshal(body, &services); err != nil {
		return 0, err
	}

	for _, svc := range services {
		if svc.ServiceName == serviceName {
			id := svc.ID
			return id, nil
		}
	}

	return 0, fmt.Errorf("service not found")
}

func (w *ResourceWatcher) handleServiceChange(obj interface{}) {
	if obj == nil {
		log.Printf("Error: received nil object in handleServiceChange")
		return
	}

	service, ok := obj.(*corev1.Service)
	if !ok {
		log.Printf("Error: unexpected type for service object")
		return
	}

	payload := w.createServicePayload(service)
	if payload.Ports == nil {
		payload.Ports = []int32{}
	}

	jsonData, err := json.Marshal(payload)
	if err != nil {
		log.Printf("Error marshaling payload: %v", err)
		return
	}

	// Try to find if the service exists
	id, err := w.findServiceID(service.Name)
	var req *http.Request
	var actionType string

	if err == nil {
		// Service exists, do PUT
		req, err = http.NewRequest(
			http.MethodPut,
			fmt.Sprintf("%s/api/service/%d", w.config.APIEndpoint, id),
			bytes.NewBuffer(jsonData),
		)
		actionType = "UPDATE"
	} else {
		// Service doesn't exist, do POST
		req, err = http.NewRequest(
			http.MethodPost,
			fmt.Sprintf("%s/api/service", w.config.APIEndpoint),
			bytes.NewBuffer(jsonData),
		)
		actionType = "CREATE"
	}

	if err != nil {
		log.Printf("Error creating request: %v", err)
		return
	}
	req.Header.Set("Content-Type", "application/json")

	log.Printf("API %s Request - Service %s/%s - URL: %s", actionType, service.Namespace, service.Name, req.URL.String())

	resp, err := w.httpClient.Do(req)
	if err != nil {
		log.Printf("Error making HTTP request: %v", err)
		return
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusCreated {
		body, _ := io.ReadAll(resp.Body)
		log.Printf("API %s Response - Service %s/%s - Status: %d, Error: %s",
			actionType, service.Namespace, service.Name, resp.StatusCode, string(body))
	} else {
		log.Printf("API %s Response - Service %s/%s - Status: %d",
			actionType, service.Namespace, service.Name, resp.StatusCode)
	}
}

func (w *ResourceWatcher) handleServiceDelete(obj interface{}) {
	service := obj.(*corev1.Service)

	id, err := w.findServiceID(service.Name)
	if err != nil {
		log.Printf("Error finding service ID: %v", err)
		return
	}

	req, err := http.NewRequest(
		http.MethodDelete,
		fmt.Sprintf("%s/api/service/%d", w.config.APIEndpoint, id),
		nil,
	)
	if err != nil {
		log.Printf("Error creating request: %v", err)
		return
	}

	log.Printf("API DELETE Request - Service %s/%s - URL: %s", service.Namespace, service.Name, req.URL.String())

	resp, err := w.httpClient.Do(req)
	if err != nil {
		log.Printf("Error making DELETE request: %v", err)
		return
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusNoContent {
		body, _ := io.ReadAll(resp.Body)
		log.Printf("API DELETE Response - Service %s/%s - Status: %d, Error: %s",
			service.Namespace, service.Name, resp.StatusCode, string(body))
	} else {
		log.Printf("API DELETE Response - Service %s/%s - Status: %d",
			service.Namespace, service.Name, resp.StatusCode)
	}
}

func main() {
	// Load application config
	appConfig, err := LoadConfig()
	if err != nil {
		log.Fatalf("Failed to load config: %v", err)
	}

	// Get kubernetes config
	k8sConfig, err := rest.InClusterConfig()
	if err != nil {
		log.Fatalf("Failed to get kubernetes config: %v", err)
	}

	// Create watcher
	watcher, err := NewResourceWatcher(k8sConfig, appConfig)
	if err != nil {
		log.Fatalf("Failed to create watcher: %v", err)
	}

	// Create context with cancellation
	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	// Start watching resources
	if err := watcher.WatchResources(ctx); err != nil {
		log.Fatalf("Error watching resources: %v", err)
	}
}
