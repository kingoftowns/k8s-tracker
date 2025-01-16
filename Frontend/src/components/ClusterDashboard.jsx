import { useState, useEffect } from 'react';
import { ChevronDown, ChevronRight, Server, Globe, Network } from 'lucide-react';

const ClusterDashboard = () => {
  const [globalSearchTerm, setGlobalSearchTerm] = useState('');

  return (
    <div className="min-h-screen bg-gray-100 p-8">
      <div className="max-w-6xl mx-auto">
        <h1 className="text-3xl font-bold text-gray-800 mb-4">Kubernetes Clusters</h1>
        
        {/* Global Search Bar */}
        <div className="mb-6 flex gap-4">
            <input
              type="text"
              placeholder="Search across all clusters..."
              value={globalSearchTerm}
              onChange={(e) => setGlobalSearchTerm(e.target.value)}
              className="flex-1 p-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
          </div>
  
          <ClusterList globalFilter={globalSearchTerm} />
        </div>
      </div>
    );
  };

const ClusterList = ({ globalFilter }) => {
  const [clusters, setClusters] = useState([]);
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(true);

  const fetchClusters = async () => {
    try {
      const response = await fetch(`${process.env.REACT_APP_CLUSTER_API_URL}/api/clusters`);
      if (!response.ok) {
        throw new Error('Failed to fetch clusters');
      }
      const data = await response.json();
      setClusters(data);
      setError(null);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchClusters();
    
    // Set up polling every 5 seconds
    const interval = setInterval(fetchClusters, 5000);
    
    // Cleanup interval on component unmount
    return () => clearInterval(interval);
  }, []);

  const hasMatchingItems = (cluster, searchTerm) => {
    const term = searchTerm.toLowerCase();
    const matchesFilter = (item) => 
      item.namespace.toLowerCase().includes(term) ||
      (item.serviceName?.toLowerCase().includes(term)) ||
      (item.ingressName?.toLowerCase().includes(term)) ||
      (item.hosts?.some(host => host.toLowerCase().includes(term))) ||
      (item.externalIp?.toLowerCase().includes(term)) ||
      (item.ports?.some(port => port.toString().includes(term)));

    return cluster.services.some(matchesFilter) || 
           cluster.ingresses.some(matchesFilter);
  };

  if (error) return <div className="text-red-500">Error: {error}</div>;
  if (loading) return <div className="text-gray-500">Loading clusters...</div>;

  return (
    <div className="space-y-4">
      {clusters.length === 0 ? (
        <div className="text-gray-500 text-center py-8">
          No clusters found
        </div>
      ) : (
        clusters.map(cluster => (
          <ClusterCard 
            key={cluster.id} 
            cluster={cluster} 
            globalFilter={globalFilter}
            autoExpand={globalFilter !== '' && hasMatchingItems(cluster, globalFilter)}
          />
        ))
      )}
    </div>
  );
};

const ClusterCard = ({ cluster, globalFilter, autoExpand }) => {
  const [isExpanded, setIsExpanded] = useState(false);
  const [localSearchTerm, setLocalSearchTerm] = useState('');
  const [selectedServiceType, setSelectedServiceType] = useState('');

  const searchTerm = localSearchTerm || globalFilter;

  useEffect(() => {
    if (autoExpand) {
      setIsExpanded(true);
    }
  }, [autoExpand]);
  
  const filterItems = (items, term, type) => {
    let filtered = items;

    // First filter by type if selected
    if (type) {
      filtered = filtered.filter(item => 
        'serviceType' in item ? item.serviceType === type : true
      );
    }

    const searchTerm = term.trim().toLowerCase();
      if (searchTerm) {
        filtered = filtered.filter(item => 
          item.namespace.toLowerCase().includes(searchTerm) ||
          (item.ingressName?.toLowerCase().includes(searchTerm)) ||
          (item.serviceName?.toLowerCase().includes(searchTerm)) ||
          (item.hosts?.some(host => host.toLowerCase().includes(searchTerm))) ||
          (item.ports?.some(port => port.toString().includes(searchTerm)))
        );
      }

    return filtered;
  };

  const filteredIngresses = filterItems(cluster.ingresses, searchTerm);
  const filteredServices = filterItems(cluster.services, searchTerm, selectedServiceType);

  return (
    <div className="bg-white rounded-lg shadow-md overflow-hidden">
      <button 
        onClick={() => setIsExpanded(!isExpanded)}
        className="w-full p-4 flex items-center justify-between hover:bg-gray-50"
      >
        <div className="flex items-center space-x-4 w-full">
          <Server className="h-6 w-6 text-blue-500 flex-shrink-0" />
          <div className="text-left w-full">
            <h2 className="text-xl font-semibold text-gray-800 mb-2">{cluster.clusterName}</h2>
            <div>
              <div className="flex items-center text-gray-600">
                <span className="w-32 text-sm font-medium">API Version:</span>
                <span className="text-sm">{cluster.apiserverVersion}</span>
              </div>
              <div className="flex items-center text-gray-600">
                <span className="w-32 text-sm font-medium">Kubelet Versions:</span>
                <span className="text-sm">[ {cluster.kubeletVersions.join(', ')} ]</span>
              </div>
              <div className="flex items-center text-gray-600">
                <span className="w-32 text-sm font-medium">Kernel Versions:</span>
                <span className="text-sm">[ {cluster.kernelVersions.join(', ')} ]</span>
              </div>
            </div>
          </div>
          {isExpanded ? 
            <ChevronDown className="h-5 w-5 text-gray-400 flex-shrink-0" /> : 
            <ChevronRight className="h-5 w-5 text-gray-400 flex-shrink-0" />
          }
        </div>
      </button>

      {isExpanded && (
          <div className="p-4 border-t border-gray-200">
            <div className="mb-4 flex gap-4">
              <input
                type="text"
                placeholder="Filter this cluster..."
                value={localSearchTerm}
                onChange={(e) => setLocalSearchTerm(e.target.value)}
                className="flex-1 p-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
              <select
                value={selectedServiceType}
                onChange={(e) => setSelectedServiceType(e.target.value)}
                className="p-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              >
                <option value="">All Service Types</option>
                <option value="ClusterIP">ClusterIP</option>
                <option value="LoadBalancer">LoadBalancer</option>
                <option value="NodePort">NodePort</option>
                <option value="ExternalName">ExternalName</option>
              </select>
            </div>

          <div className="grid grid-cols-2 gap-4">
            {/* Ingresses section */}
            <div>
              <h3 className="flex items-center text-lg font-semibold text-gray-800 mb-3">
                <Globe className="h-5 w-5 text-green-500 mr-2" />
                Ingresses ({filteredIngresses.length})
              </h3>
              <div className="space-y-2">
                {filteredIngresses.map(ingress => (
                  <div key={ingress.id} className="p-3 bg-gray-50 rounded-md">
                    <div className="font-medium text-gray-800">{ingress.ingressName}</div>
                    <div className="text-sm text-gray-500">Namespace: {ingress.namespace}</div>
                    <div className="text-sm text-gray-500">
                      Hosts: {ingress.hosts.join(', ')}
                    </div>
                    {ingress.ports && ingress.ports.length > 0 && (
                      <div className="text-sm text-gray-500">
                        Ports: {ingress.ports.join(', ')}
                      </div>
                    )}
                  </div>
                ))}
                {filteredIngresses.length === 0 && (
                  <div className="text-gray-500 text-sm">
                    {searchTerm ? "No matching ingresses found" : "No ingresses found"}
                  </div>
                )}
              </div>
            </div>

            {/* Services section */}
            <div>
              <h3 className="flex items-center text-lg font-semibold text-gray-800 mb-3">
                <Network className="h-5 w-5 text-purple-500 mr-2" />
                Services ({filteredServices.length})
              </h3>
              <div className="space-y-2">
                {filteredServices.map(service => (
                  <div key={service.id} className="p-3 bg-gray-50 rounded-md">
                    <div className="font-medium text-gray-800">{service.serviceName}</div>
                    <div className="text-sm text-gray-500">
                      <div>Namespace: {service.namespace}</div>
                      <div>Type: {service.serviceType}</div>
                      {service.externalIp && (
                        <div>External IP: {service.externalIp}</div>
                      )}
                      <div>Ports: {service.ports.join(', ')}</div>
                    </div>
                  </div>
                ))}
                {filteredServices.length === 0 && (
                  <div className="text-gray-500 text-sm">
                    {searchTerm || selectedServiceType ? "No matching services found" : "No services found"}
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default ClusterDashboard;