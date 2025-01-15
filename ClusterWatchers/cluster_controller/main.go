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
}

type ServiceResponse struct {
	ID          int     `json:"id"`
	ClusterName string  `json:"clusterName"`
	Namespace   string  `json:"namespace"`
	ServiceName string  `json:"serviceName"`
	ExternalIP  string  `json:"externalIp"`
	Ports       []int32 `json:"ports"`
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
					InsecureSkipVerify: true, // Only use this if you're sure about your endpoint
				},
			},
		},
		config: appConfig,
	}, nil
}

func (w *ResourceWatcher) WatchResources(ctx context.Context) error {
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
			AddFunc:    w.handleIngressAdd,
			UpdateFunc: w.handleIngressUpdate,
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
			AddFunc:    w.handleServiceAdd,
			UpdateFunc: w.handleServiceUpdate,
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

func (w *ResourceWatcher) handleIngressAdd(obj interface{}) {
	if obj == nil {
		log.Printf("Error: received nil object in handleIngressAdd")
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

	resp, err := w.httpClient.Post(
		fmt.Sprintf("%s/api/ingress", w.config.APIEndpoint),
		"application/json",
		bytes.NewBuffer(jsonData),
	)
	if err != nil {
		log.Printf("Error making POST request: %v", err)
		return
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusCreated {
		body, _ := io.ReadAll(resp.Body)
		log.Printf("Unexpected status code: %d, body: %s", resp.StatusCode, string(body))
	}
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

func (w *ResourceWatcher) handleIngressUpdate(oldObj, newObj interface{}) {
	ingress := newObj.(*networkingv1.Ingress)

	// Find the ingress ID
	id, err := w.findIngressID(ingress.Name)
	if err != nil {
		log.Printf("Error finding ingress ID: %v", err)
		return
	}

	// Create and send update request
	payload := w.createIngressPayload(ingress)
	jsonData, err := json.Marshal(payload)
	if err != nil {
		log.Printf("Error marshaling payload: %v", err)
		return
	}

	req, err := http.NewRequest(
		http.MethodPut,
		fmt.Sprintf("%s/api/ingress/%d", w.config.APIEndpoint, id),
		bytes.NewBuffer(jsonData),
	)
	if err != nil {
		log.Printf("Error creating request: %v", err)
		return
	}
	req.Header.Set("Content-Type", "application/json")

	resp, err := w.httpClient.Do(req)
	if err != nil {
		log.Printf("Error making PUT request: %v", err)
		return
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		log.Printf("Unexpected status code: %d", resp.StatusCode)
	}
}

func (w *ResourceWatcher) handleIngressDelete(obj interface{}) {
	ingress := obj.(*networkingv1.Ingress)

	// Find the ingress ID
	id, err := w.findIngressID(ingress.Name)
	if err != nil {
		log.Printf("Error finding ingress ID: %v", err)
		return
	}

	// Send delete request
	req, err := http.NewRequest(
		http.MethodDelete,
		fmt.Sprintf("%s/api/ingress/%d", w.config.APIEndpoint, id),
		nil,
	)
	if err != nil {
		log.Printf("Error creating request: %v", err)
		return
	}

	resp, err := w.httpClient.Do(req)
	if err != nil {
		log.Printf("Error making DELETE request: %v", err)
		return
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusNoContent {
		log.Printf("Unexpected status code: %d", resp.StatusCode)
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

	return ServicePayload{
		ClusterName: w.clusterName,
		Namespace:   service.Namespace,
		ServiceName: service.Name,
		ExternalIP:  externalIP,
		Ports:       ports,
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

func (w *ResourceWatcher) handleServiceAdd(obj interface{}) {
	if obj == nil {
		log.Printf("Error: received nil object in handleServiceAdd")
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

	resp, err := w.httpClient.Post(
		fmt.Sprintf("%s/api/service", w.config.APIEndpoint),
		"application/json",
		bytes.NewBuffer(jsonData),
	)
	if err != nil {
		log.Printf("Error making POST request: %v", err)
		return
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusCreated {
		body, _ := io.ReadAll(resp.Body)
		log.Printf("Unexpected status code: %d, body: %s", resp.StatusCode, string(body))
	}
}

func (w *ResourceWatcher) handleServiceUpdate(oldObj, newObj interface{}) {
	service := newObj.(*corev1.Service)

	// Find the service ID
	id, err := w.findServiceID(service.Name)
	if err != nil {
		log.Printf("Error finding service ID: %v", err)
		return
	}

	// Create and send update request
	payload := w.createServicePayload(service)
	jsonData, err := json.Marshal(payload)
	if err != nil {
		log.Printf("Error marshaling payload: %v", err)
		return
	}

	req, err := http.NewRequest(
		http.MethodPut,
		fmt.Sprintf("%s/api/service/%d", w.config.APIEndpoint, id),
		bytes.NewBuffer(jsonData),
	)
	if err != nil {
		log.Printf("Error creating request: %v", err)
		return
	}
	req.Header.Set("Content-Type", "application/json")

	resp, err := w.httpClient.Do(req)
	if err != nil {
		log.Printf("Error making PUT request: %v", err)
		return
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		log.Printf("Unexpected status code: %d", resp.StatusCode)
	}
}

func (w *ResourceWatcher) handleServiceDelete(obj interface{}) {
	service := obj.(*corev1.Service)

	// Find the service ID
	id, err := w.findServiceID(service.Name)
	if err != nil {
		log.Printf("Error finding service ID: %v", err)
		return
	}

	// Send delete request
	req, err := http.NewRequest(
		http.MethodDelete,
		fmt.Sprintf("%s/api/service/%d", w.config.APIEndpoint, id),
		nil,
	)
	if err != nil {
		log.Printf("Error creating request: %v", err)
		return
	}

	resp, err := w.httpClient.Do(req)
	if err != nil {
		log.Printf("Error making DELETE request: %v", err)
		return
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusNoContent {
		log.Printf("Unexpected status code: %d", resp.StatusCode)
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
