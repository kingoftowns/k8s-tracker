import { useState, useEffect } from 'react';
import { ChevronDown, ChevronRight, Server, Globe, Network } from 'lucide-react';

const ClusterDashboard = () => {
  return (
    <div className="min-h-screen bg-gray-100 p-8">
      <div className="max-w-6xl mx-auto">
        <h1 className="text-3xl font-bold text-gray-800 mb-8">Kubernetes Clusters</h1>
        <ClusterList />
      </div>
    </div>
  );
};

const ClusterList = () => {
  const [clusters, setClusters] = useState([]);
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(true);

  const fetchClusters = async () => {
    try {
      const response = await fetch('https://cluster-info.k8s.blacktoaster.com/api/clusters');
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

  if (error) return <div className="text-red-500">Error: {error}</div>;
  if (loading) return <div className="text-gray-500">Loading clusters...</div>;

  return (
    <div className="space-y-4">
      {clusters.map(cluster => (
        <ClusterCard key={cluster.id} cluster={cluster} />
      ))}
    </div>
  );
};

const ClusterCard = ({ cluster }) => {
  const [isExpanded, setIsExpanded] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');

  // Filter function
  const filterItems = (items, term) => {
    if (!term.trim()) return items;
    const loweredTerm = term.toLowerCase();
    return items.filter(item => 
      item.namespace.toLowerCase().includes(loweredTerm) ||
      (item.ingressName?.toLowerCase().includes(loweredTerm)) ||
      (item.serviceName?.toLowerCase().includes(loweredTerm)) ||
      (item.hosts?.some(host => host.toLowerCase().includes(loweredTerm))) ||
      (item.ports?.some(port => port.toString().includes(term)))
    );
  };

  const filteredIngresses = filterItems(cluster.ingresses, searchTerm);
  const filteredServices = filterItems(cluster.services, searchTerm);

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
          {/* Search Input */}
          <div className="mb-4">
            <input
              type="text"
              placeholder="Filter services and ingresses..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full p-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
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

            <div>
              <h3 className="flex items-center text-lg font-semibold text-gray-800 mb-3">
                <Network className="h-5 w-5 text-purple-500 mr-2" />
                Services ({filteredServices.length})
              </h3>
              <div className="space-y-2">
                {filteredServices.map(service => (
                  <div key={service.id} className="p-3 bg-gray-50 rounded-md">
                    <div className="font-medium text-gray-800">{service.serviceName}</div>
                    <div className="text-sm text-gray-500">Namespace: {service.namespace}</div>
                    {service.externalIp && (
                      <div className="text-sm text-gray-500">
                        External IP: {service.externalIp}
                      </div>
                    )}
                    <div className="text-sm text-gray-500">
                      Ports: {service.ports.join(', ')}
                    </div>
                  </div>
                ))}
                {filteredServices.length === 0 && (
                  <div className="text-gray-500 text-sm">
                    {searchTerm ? "No matching services found" : "No services found"}
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