// App.js
import React from 'react';
import { BrowserRouter as Router, Route, Routes, Navigate } from 'react-router-dom';
import Layout from './components/Layout';
import UploadPage from './pages/UploadPage';

function App() {
  return (
    <Router>
      <Layout>
        <Routes>
          <Route path="/upload" element={<UploadPage />} />
          <Route path="*" element={<Navigate to="/upload" replace />} /> {/* Redirects all routes to /upload */}
        </Routes>
      </Layout>
    </Router>
  );
}

export default App;