import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Search, ZoomIn, ZoomOut, CheckCircle, XCircle } from 'lucide-react';
import { getInvoiceById, getNextAwaitingReviewInvoice } from '../utils/invoiceUtils';
import { formatAsDollars, formatFieldName } from '../utils/formatUtils';

const ReviewPage = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const [invoice, setInvoice] = useState(null);
  const [detailedView, setDetailedView] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(true);
  const [showApproved, setShowApproved] = useState(false);
  const [showRejected, setShowRejected] = useState(false);

  const approveInvoice = useCallback(() => {
    setShowApproved(true);
    setTimeout(() => {
      setShowApproved(false);
      const nextInvoiceId = getNextAwaitingReviewInvoice(id);
      if (nextInvoiceId) {
        navigate(`/review/${nextInvoiceId}`);
      } else {
        navigate('/queue');
      }
    }, 2000);
  }, [id, navigate]);

  const rejectInvoice = useCallback(() => {
    setShowRejected(true);
    setTimeout(() => {
      setShowRejected(false);
      const nextInvoiceId = getNextAwaitingReviewInvoice(id);
      if (nextInvoiceId) {
        navigate(`/review/${nextInvoiceId}`);
      } else {
        navigate('/queue');
      }
    }, 2000);
  }, [id, navigate]); // Fixed the closing bracket and dependencies array

  const handleKeyDown = useCallback((event) => {
    if (document.activeElement.id !== 'searchInput') {
      if (event.key === 'd') {
        setDetailedView(prev => !prev);
      } else if (event.key === '/') {
        event.preventDefault();
        document.getElementById('searchInput').focus();
      } else if (event.key === 'a') {
        approveInvoice();
      } else if (event.key === 'r') {
        rejectInvoice();
      } else if (event.key === 'p') {
        const prevInvoiceId = getNextAwaitingReviewInvoice(id, 'previous');
        if (prevInvoiceId) navigate(`/review/${prevInvoiceId}`);
      } else if (event.key === 'n') {
        const nextInvoiceId = getNextAwaitingReviewInvoice(id, 'next');
        if (nextInvoiceId) navigate(`/review/${nextInvoiceId}`);
      } else if (event.key === 'g') {
        const handleSecondKey = (e) => {
          if (e.key === 'u') navigate('/upload');
          else if (e.key === 'r') navigate(`/review/${id}`);
          else if (e.key === 'q') navigate('/queue');
          document.removeEventListener('keydown', handleSecondKey);
        };
        document.addEventListener('keydown', handleSecondKey);
      }
    }
  }, [approveInvoice, rejectInvoice, navigate, id]);

  useEffect(() => {
    const fetchInvoice = async () => {
      setLoading(true);
      setError(null);
      try {
        const fetchedInvoice = await getInvoiceById(id);
        if (fetchedInvoice) {
          setInvoice(fetchedInvoice);
          console.log('Fetched invoice:', fetchedInvoice);
        } else {
          throw new Error(`Invoice with ID ${id} not found`);
        }
      } catch (err) {
        console.error('Error fetching invoice:', err);
        setError(err.message);
      } finally {
        setLoading(false);
      }
    };

    fetchInvoice();

    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [id, handleKeyDown]);

  const getConfidenceColor = (score) => {
    if (score >= 0.9) return 'bg-green-500';
    if (score >= 0.8) return 'bg-yellow-500';
    return 'bg-red-500';
  };

  const highlightSearchTerm = (text) => {
    if (!searchTerm) return text;
    const parts = text.toString().split(new RegExp(`(${searchTerm})`, 'gi'));
    return parts.map((part, index) => 
      part.toLowerCase() === searchTerm.toLowerCase() 
        ? <span key={index} className="bg-yellow-200">{part}</span>
        : part
    );
  };

  const ConfidenceBar = ({ confidence }) => (
    <div className="flex items-center">
      <div className="w-16 bg-gray-200 rounded-full h-2 mr-1">
        <div className={`h-2 rounded-full ${getConfidenceColor(confidence)}`} style={{ width: `${confidence * 100}%` }}></div>
      </div>
      <span className="text-xs">{(confidence * 100).toFixed(0)}%</span>
    </div>
  );

  if (loading) {
    return <div className="bg-white shadow rounded-lg p-6">Loading...</div>;
  }

  if (error) {
    return <div className="bg-white shadow rounded-lg p-6">Error: {error}</div>;
  }

  if (!invoice) {
    return <div className="bg-white shadow rounded-lg p-6">No invoice found.</div>;
  }

  return (
    <div className="bg-white shadow rounded-lg p-6 relative">
      {showApproved && (
        <div className="absolute inset-0 flex items-center justify-center bg-white bg-opacity-75 z-50">
          <div className="bg-green-100 border-l-4 border-green-500 text-green-700 p-4 rounded shadow-lg flex items-center">
            <CheckCircle className="mr-2" />
            Invoice Approved
          </div>
        </div>
      )}
      {showRejected && (
        <div className="absolute inset-0 flex items-center justify-center bg-white bg-opacity-75 z-50">
          <div className="bg-red-100 border-l-4 border-red-500 text-red-700 p-4 rounded shadow-lg flex items-center">
            <XCircle className="mr-2" />
            Invoice Rejected
          </div>
        </div>
      )}
      <div className="flex justify-between items-center mb-4">
        <div>
          <h2 className="text-xl font-semibold text-gray-700">Invoice Review</h2>
          <p className="text-sm text-gray-500">Supplier: {invoice.supplierName}</p>
          <p className="text-sm text-gray-500">Invoice Number: {invoice.invNumber}</p>
        </div>
        <div className="flex space-x-4">
          <button className="px-4 py-2 bg-green-500 text-white rounded hover:bg-green-600" onClick={approveInvoice}>
            Approve (A)
          </button>
          <button className="px-4 py-2 bg-red-500 text-white rounded hover:bg-red-600" onClick={rejectInvoice}>
            Reject (R)
          </button>
        </div>
      </div>

      <div className="flex justify-between items-center mb-6">
        <div className="flex items-center space-x-4">
          <button 
            onClick={() => {
              const prevInvoiceId = getNextAwaitingReviewInvoice(id, 'previous');
              if (prevInvoiceId) navigate(`/review/${prevInvoiceId}`);
            }}
            className="px-3 py-1 bg-gray-200 hover:bg-gray-300 text-gray-600 rounded text-sm"
          >
            Previous (P)
          </button>
          <button 
            onClick={() => {
              const nextInvoiceId = getNextAwaitingReviewInvoice(id, 'next');
              if (nextInvoiceId) navigate(`/review/${nextInvoiceId}`);
            }}
            className="px-3 py-1 bg-gray-200 hover:bg-gray-300 text-gray-600 rounded text-sm"
          >
            Next (N)
          </button>
          <div className="relative">
            <input
              id="searchInput"
              type="text"
              placeholder="Search invoice... (/)"
              className="pl-10 pr-4 py-2 border rounded-md w-64 focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
            <Search className="absolute left-3 top-2.5 text-gray-400" size={20} />
          </div>
          <button 
            onClick={() => setDetailedView(!detailedView)}
            className="px-4 py-2 bg-gray-200 hover:bg-gray-300 text-gray-700 rounded"
          >
            {detailedView ? 'Hide' : 'Show'} Details (D)
          </button>
        </div>
      </div>

      <div className="flex flex-col md:flex-row gap-6 mb-6">
        <div className="w-full md:w-1/2 bg-gray-100 p-4 rounded-lg">
          <h3 className="text-lg font-semibold mb-4">Invoice Details</h3>
          <div className="grid grid-cols-2 gap-4">
            {Object.entries(invoice.details).map(([key, { value, confidence }]) => (
              <div key={key}>
                <p className="text-sm font-medium text-gray-500">{formatFieldName(key)}</p>
                <p className="text-sm text-gray-900">
                  {key.toLowerCase().includes('amount') ? formatAsDollars(value) : highlightSearchTerm(value)}
                </p>
                {detailedView && <ConfidenceBar confidence={confidence} />}
              </div>
            ))}
          </div>
        </div>
        
        <div className="w-full md:w-1/2 bg-gray-100 p-4 rounded-lg">
          <h3 className="text-lg font-semibold mb-4">PDF Preview</h3>
          <div className="border border-gray-300 rounded-lg overflow-hidden">
            <div className="bg-gray-200 p-2 flex justify-between items-center">
              <div>
                Page 1 of 1
              </div>
              <div className="flex gap-2">
                <button className="p-1 bg-white rounded hover:bg-gray-100">
                  <ZoomOut size={20} />
                </button>
                <button className="p-1 bg-white rounded hover:bg-gray-100">
                  <ZoomIn size={20} />
                </button>
              </div>
            </div>
            <div className="flex justify-center items-center bg-gray-100 h-96">
              <p className="text-gray-500">PDF Preview Placeholder</p>
            </div>
            <div className="bg-gray-200 p-2 flex justify-center items-center">
              <button 
                className="px-2 py-1 bg-white rounded mr-2 opacity-50 cursor-not-allowed"
                disabled
              >
                Previous
              </button>
              <button 
                className="px-2 py-1 bg-white rounded opacity-50 cursor-not-allowed"
                disabled
              >
                Next
              </button>
            </div>
          </div>
        </div>
      </div>

      <div className="bg-gray-100 p-4 rounded-lg mt-6">
        <h3 className="text-lg font-semibold mb-4">Line Items</h3>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="bg-blue-100">
                <th className="px-4 py-2 text-left">Coding</th>
                <th className="px-4 py-2 text-right">Amount</th>
                {detailedView && (
                  <>
                    <th className="px-4 py-2 text-left">Description</th>
                    <th className="px-4 py-2 text-right">Quantity</th>
                    <th className="px-4 py-2 text-right">Unit Price</th>
                    <th className="px-4 py-2 text-center">Confidence</th>
                  </>
                )}
              </tr>
            </thead>
            <tbody>
              {Object.entries(invoice.lineItems.reduce((acc, item) => {
                const key = `${item.costCategory} | ${item.spendCategory} | ${item.jobCode}`;
                if (!acc[key]) {
                  acc[key] = { items: [], amount: 0, quantity: 0 };
                }
                acc[key].items.push(item);
                acc[key].amount += item.amount;
                acc[key].quantity += item.quantity;
                return acc;
              }, {})).map(([key, category], index) => (
                <React.Fragment key={key}>
                  <tr className={`${index % 2 === 0 ? 'bg-white' : 'bg-gray-50'} hover:bg-blue-50 transition-colors duration-150`}>
                    <td className="px-4 py-2 font-medium">
                      {detailedView ? (
                        <div>
                          <div>Cost Category: {highlightSearchTerm(key.split(' | ')[0])}</div>
                          <div>Spend Category: {highlightSearchTerm(key.split(' | ')[1])}</div>
                          <div>Job Code: {highlightSearchTerm(key.split(' | ')[2])}</div>
                        </div>
                      ) : (
                        highlightSearchTerm(key)
                      )}
                    </td>
                    <td className="px-4 py-2 text-right font-semibold text-blue-700">{formatAsDollars(category.amount)}</td>
                    {detailedView && <td colSpan="4"></td>}
                  </tr>
                  {detailedView && category.items.map((item, itemIndex) => (
                    <tr key={itemIndex} className={`${index % 2 === 0 ? 'bg-white' : 'bg-gray-50'} hover:bg-blue-50 transition-colors duration-150`}>
                      <td className="px-4 py-2 pl-8"></td>
                      <td className="px-4 py-2 text-right text-blue-600">{formatAsDollars(item.amount)}</td>
                      <td className="px-4 py-2">{highlightSearchTerm(item.description)}</td>
                      <td className="px-4 py-2 text-right">{item.quantity}</td>
                      <td className="px-4 py-2 text-right">{formatAsDollars(item.unitPrice)}</td>
                      <td className="px-4 py-2">
                        <div className="flex items-center justify-center">
                          <div className="w-16 bg-gray-200 rounded-full h-2 mr-2">
                            <div 
                              className={`h-2 rounded-full ${
                                item.confidence >= 0.9 ? 'bg-green-500' : 
                                item.confidence >= 0.7 ? 'bg-yellow-500' : 'bg-red-500'
                              }`} 
                              style={{ width: `${item.confidence * 100}%` }}
                            ></div>
                          </div>
                          <span className={`text-xs font-medium ${
                            item.confidence >= 0.9 ? 'text-green-600' : 
                            item.confidence >= 0.7 ? 'text-yellow-600' : 'text-red-600'
                          }`}>
                            {(item.confidence * 100).toFixed(0)}%
                          </span>
                        </div>
                      </td>
                    </tr>
                  ))}
                </React.Fragment>
              ))}
            </tbody>
            <tfoot>
              <tr className="bg-blue-200 font-semibold">
                <td className="px-4 py-2">Total</td>
                <td className="px-4 py-2 text-right text-blue-800">
                  {formatAsDollars(invoice.lineItems.reduce((total, item) => total + item.amount, 0))}
                </td>
                {detailedView && <td colSpan="4"></td>}
              </tr>
            </tfoot>
          </table>
        </div>
      </div>

      <div className="mt-6 text-sm text-gray-500">
        <p>Keyboard shortcuts:</p>
        <ul className="grid grid-cols-2 gap-2 mt-2">
          <li>A - Approve invoice</li>
          <li>R - Reject invoice</li>
          <li>D - Toggle detailed view</li>
          <li>/ - Focus search</li>
          <li>N - Next invoice</li>
          <li>P - Previous invoice</li>
          <li>G + U - Go to upload</li>
          <li>G + Q - Go to queue</li>
        </ul>
      </div>
    </div>
  );
};

export default ReviewPage;