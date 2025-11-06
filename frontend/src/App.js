import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Login from './Login';
import DevicesList from './DevicesList';
import DeviceLogs from './DeviceLogs';
import Layout from './components/Layout';
import './App.css';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/" element={<Navigate to="/devices" replace />} />
        
        {/* Protected routes */}
        <Route element={<Layout />}>
          <Route path="/devices" element={<DevicesList />} />
          <Route path="/device/:deviceCode" element={<DeviceLogs />} />
        </Route>
        
        {/* Redirect any unknown paths to /devices */}
        <Route path="*" element={<Navigate to="/devices" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;