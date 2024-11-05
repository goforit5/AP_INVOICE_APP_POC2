import React, { useState, useRef, useCallback, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Upload } from 'lucide-react';
import { uploadInvoice } from '../services/api';

const UploadPage = () => {
  const navigate = useNavigate();
  const [isDragging, setIsDragging] = useState(false);
  const [uploadedFiles, setUploadedFiles] = useState([]);
  const [uploadProgress, setUploadProgress] = useState({});
  const fileInputRef = useRef(null);

  const handleDragEnter = (e) => {
    e.preventDefault();
    setIsDragging(true);
  };

  const handleKeyDown = useCallback((event) => {
    if (event.key === 'u') {
      fileInputRef.current.click();
    } else if (event.key === 'g') {
      const handleSecondKey = (e) => {
        if (e.key === 'u') {} // Already on upload page
        else if (e.key === 'r') {
          // Navigate to review page of first awaiting review invoice
        }
        else if (e.key === 'q') navigate('/queue');
        document.removeEventListener('keydown', handleSecondKey);
      };
      document.addEventListener('keydown', handleSecondKey);
    }
  }, []);

  useEffect(() => {
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [handleKeyDown]);

  const handleDragLeave = (e) => {
    e.preventDefault();
    setIsDragging(false);
  };

  const handleDrop = (e) => {
    e.preventDefault();
    setIsDragging(false);
    const files = Array.from(e.dataTransfer.files);
    handleFiles(files);
  };

  const handleFiles = (files) => {
    files.forEach(file => {
      setUploadedFiles(prev => [...prev, file]);
      uploadFile(file);
    });
  };

  const handleFileInputChange = (e) => {
    handleFiles(Array.from(e.target.files));
  };

  const uploadFile = async (file) => {
    try {
      const response = await uploadInvoice(file);
      setUploadProgress(prev => ({ ...prev, [file.name]: 100 }));
      console.log('File uploaded successfully:', response.data);
    } catch (error) {
      console.error('Error uploading file:', error);
      setUploadProgress(prev => ({ ...prev, [file.name]: 'Error' }));
    }
  };

  return (
    <div className="bg-white shadow rounded-lg p-6">
      <h2 className="text-xl font-semibold text-gray-700 mb-6">Upload Invoices</h2>

      <div 
        onDragEnter={handleDragEnter}
        onDragOver={handleDragEnter}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        className={`border-2 border-dashed rounded-lg p-10 text-center cursor-pointer transition-colors duration-300 ${
          isDragging ? 'border-blue-500 bg-blue-50' : 'border-gray-300 hover:border-gray-400'
        }`}
      >
        <Upload className="mx-auto h-12 w-12 text-gray-400" />
        <p className="mt-1 text-sm text-gray-600">Drag and drop invoice files here, or click to select files</p>
        <input
          type="file"
          multiple
          onChange={handleFileInputChange}
          className="hidden"
          id="fileInput"
          ref={fileInputRef}
        />
        <label
          htmlFor="fileInput"
          className="mt-2 inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
        >
          Select Files
        </label>
      </div>

      {uploadedFiles.length > 0 && (
        <div className="mt-8">
          <h3 className="text-lg font-medium text-gray-900 mb-4">Uploaded Files</h3>
          <ul className="divide-y divide-gray-200">
            {uploadedFiles.map((file, index) => (
              <li key={index} className="py-4">
                <div className="flex items-center justify-between">
                  <div className="flex items-center">
                    <Upload className="h-5 w-5 text-gray-400 mr-2" />
                    <span className="text-sm font-medium text-gray-900">{file.name}</span>
                  </div>
                  <span className="text-sm text-gray-500">
                    {uploadProgress[file.name] === 100 ? 'Completed' : 'Uploading...'}
                  </span>
                </div>
                <div className="mt-1">
                  <div className="bg-gray-200 rounded-full h-2.5 dark:bg-gray-700 w-full">
                    <div 
                      className="bg-blue-600 h-2.5 rounded-full transition-all duration-500" 
                      style={{ width:  }}
                    ></div>
                  </div>
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
};

export default UploadPage;
