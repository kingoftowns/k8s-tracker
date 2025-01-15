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

  return (
    <div className="bg-white rounded-lg shadow-md overflow-hidden">
      <button 
        onClick={() => setIsExpanded(!isExpanded)}
        className="w-full p-4 flex items-center justify-between hover:bg-gray-50"
      >
        <div className="flex items-center space-x-3">
          <Server className="h-5 w-5 text-blue-500" />
          <div className="text-left">
            <h2 className="text-lg font-semibold text-gray-800">{cluster.clusterName}</h2>
            <p className="text-sm text-gray-500">API Version: {cluster.apiserverVersion}</p>
            <div className="text-sm text-gray-500">
              <p>Kubelet Versions: {cluster.kubeletVersions.join(', ')}</p>
              <p>Kernel Versions: {cluster.kernelVersions.join(', ')}</p>
            </div>
          </div>
        </div>
        {isExpanded ? 
          <ChevronDown className="h-5 w-5 text-gray-400" /> : 
          <ChevronRight className="h-5 w-5 text-gray-400" />
        }
      </button>

      {isExpanded && (
        <div className="p-4 border-t border-gray-200">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <h3 className="flex items-center text-lg font-semibold text-gray-800 mb-3">
                <Globe className="h-5 w-5 text-green-500 mr-2" />
                Ingresses
              </h3>
              <div className="space-y-2">
                {cluster.ingresses.map(ingress => (
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
                {cluster.ingresses.length === 0 && (
                  <div className="text-gray-500 text-sm">No ingresses found</div>
                )}
              </div>
            </div>

            <div>
              <h3 className="flex items-center text-lg font-semibold text-gray-800 mb-3">
                <Network className="h-5 w-5 text-purple-500 mr-2" />
                Services
              </h3>
              <div className="space-y-2">
                {cluster.services.map(service => (
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
                {cluster.services.length === 0 && (
                  <div className="text-gray-500 text-sm">No services found</div>
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