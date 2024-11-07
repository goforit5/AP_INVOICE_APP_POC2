import React, { useState, useEffect, useCallback } from 'react';
import { Search } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import queueData from '../data/queue.json';
import { formatAsDollars } from '../utils/formatUtils';

const QueuePage = () => {
  const [invoices, setInvoices] = useState([]);
  const navigate = useNavigate();
  const [detailedView, setDetailedView] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');

  const handleKeyDown = useCallback((event) => {
    if (document.activeElement.id !== 'searchInput') {
      if (event.key === 'd') {
        setDetailedView(prev => !prev);
      } else if (event.key === '/') {
        event.preventDefault();
        document.getElementById('searchInput').focus();
      } else if (event.key === 'Enter') {
        const currentFilteredInvoices = invoices.filter(invoice =>
          Object.values(invoice).some(value => 
            value.toString().toLowerCase().includes(searchTerm.toLowerCase())
          )
        );
        if (currentFilteredInvoices.length > 0) {
          navigate(`/review/${currentFilteredInvoices[0].id}`);
        }
      } else if (event.key === 'g') {
        const handleSecondKey = (e) => {
          if (e.key === 'u') navigate('/upload');
          else if (e.key === 'r') {
            const awaitingReviewInvoice = invoices.find(inv => inv.status === 'Awaiting Review');
            if (awaitingReviewInvoice) {
              navigate(`/review/${awaitingReviewInvoice.id}`);
            }
          }
          else if (e.key === 'q') navigate('/queue');
          document.removeEventListener('keydown', handleSecondKey);
        };
        document.addEventListener('keydown', handleSecondKey);
      }
    }
  }, [invoices, searchTerm, navigate]);

  useEffect(() => {
    setInvoices(queueData);
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [handleKeyDown]);

  const getStatusColor = (status) => {
    switch (status) {
      case 'Processed': return 'bg-green-100 text-green-800';
      case 'Awaiting Review': return 'bg-blue-100 text-blue-800';
      case 'Needs Attention': return 'bg-orange-100 text-orange-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  const getConfidenceColor = (score) => {
    if (score >= 0.9) return 'bg-green-500';
    if (score >= 0.8) return 'bg-yellow-500';
    return 'bg-red-500';
  };

  const filteredInvoices = invoices.filter(invoice =>
    Object.values(invoice).some(value => 
      value.toString().toLowerCase().includes(searchTerm.toLowerCase())
    )
  );

  return (
    <div className="bg-white shadow rounded-lg p-6">
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-xl font-semibold text-gray-700">Invoice Queue</h2>
        <div className="flex items-center space-x-4">
          <div className="relative">
            <input
              id="searchInput"
              type="text"
              placeholder="Search invoices..."
              className="pl-10 pr-4 py-2 border rounded-md w-64"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
            <Search className="absolute left-3 top-2.5 text-gray-400" size={20} />
          </div>
          <button 
            onClick={() => setDetailedView(!detailedView)}
            className="px-4 py-2 bg-gray-200 text-gray-700 rounded"
          >
            {detailedView ? 'Hide' : 'Show'} Details
          </button>
        </div>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-1/5">Supplier Name</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-1/5">Inv Date</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-1/5">Inv Number</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-1/5">Inv Amount</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-1/5">Status</th>
              {detailedView && <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">AI Confidence</th>}
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {filteredInvoices.map((invoice) => (
              <tr key={invoice.id} className="cursor-pointer hover:bg-gray-50" onClick={() => navigate(`/review/${invoice.id}`)}>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 truncate w-1/5">{invoice.supplierName}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 w-1/5">{invoice.invDate}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 w-1/5">{invoice.invNumber}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 w-1/5">{formatAsDollars(invoice.invAmount)}</td>
                <td className="px-6 py-4 whitespace-nowrap w-1/5">
                  <span className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${getStatusColor(invoice.status)}`}>
                    {invoice.status}
                  </span>
                </td>
                {detailedView && (
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center">
                      <div className="w-16 bg-gray-200 rounded-full h-2 mr-2">
                        <div className={`h-2 rounded-full ${getConfidenceColor(invoice.aiConfidence)}`} style={{ width: `${invoice.aiConfidence * 100}%` }}></div>
                      </div>
                      <span className="text-sm font-medium text-gray-900">{(invoice.aiConfidence * 100).toFixed(0)}%</span>
                    </div>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default QueuePage;