import axios from 'axios';

const api = axios.create({
    baseURL: process.env.REACT_APP_API_BASE_URL || 'http://localhost:5097/api'
});

// Function to upload a file to the backend
export const uploadInvoice = (file) => {
    const formData = new FormData();
    formData.append("file", file);
    return api.post("/invoice", formData, {
        headers: {
            "Content-Type": "multipart/form-data"
        }
    });
};
