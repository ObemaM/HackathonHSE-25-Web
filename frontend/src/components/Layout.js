import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { logout, getCurrentUser, fetchCurrentUserSMPs } from '../api';
import { useEffect, useState, useRef } from 'react';

function Layout() {
  const navigate = useNavigate();
  const location = useLocation();
  const [isLoading, setIsLoading] = useState(true);
  const [user, setUser] = useState(null);
  const [smps, setSmps] = useState([]);
  const [isDropdownOpen, setIsDropdownOpen] = useState(false);
  const dropdownRef = useRef(null);

  // Закрытие выпадающего меню при клике вне его области
  useEffect(() => {
    function handleClickOutside(event) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
        setIsDropdownOpen(false);
      }
    }

    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, []);

  // Получение данные пользователя и его СМП
  useEffect(() => {
    const checkAuth = async () => {
      try {
        const userData = await getCurrentUser();
        setUser(userData);
        
        // Получение СМП пользователя
        try {
          const userSmps = await fetchCurrentUserSMPs();
          setSmps(userSmps);
        } catch (error) {
          console.error('Ошибка при загрузке СМП пользователя:', error);
          setSmps([]);
        }
        
        setIsLoading(false);
      } catch (error) {
        if (location.pathname !== '/login') {
          navigate('/login');
        } else {
          setIsLoading(false);
        }
      }
    };

    checkAuth();
  }, [navigate, location.pathname]);

  const handleLogout = async () => {
    try {
      await logout();
      setUser(null);
      navigate('/login');
    } catch (error) {
      console.error('Logout failed:', error);
    }
  };

  if (isLoading) {
    return (
      <div style={{ 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'center', 
        height: '100vh' 
      }}>
        <div>Загрузка...</div>
      </div>
    );
  }

  if (location.pathname === '/login') {
    return <Outlet />;
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <header style={{
        background: 'linear-gradient(135deg, #B0C4DE 0%, #372F85 100%)',
        padding: '0.75rem 1.5rem',
        boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
        position: 'sticky',
        top: 0,
        zIndex: 1000,
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        height: '60px',
        boxSizing: 'border-box'
      }}>
        <h1 style={{ 
          margin: 0, 
          fontSize: '1.25rem',
          fontWeight: '600',
          color: 'white',
          textShadow: '0 1px 2px rgba(0, 0, 0, 0.6)' // Тень для чёткости
        }}>
          Мониторинг устройств
        </h1>
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          {user && (
            <div style={{ position: 'relative', display: 'inline-block' }} ref={dropdownRef}>
              <div 
                onClick={() => smps.length > 0 && setIsDropdownOpen(!isDropdownOpen)}
                style={{ 
                  fontSize: '0.9rem',
                  cursor: smps.length > 0 ? 'pointer' : 'default',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '0.3rem',
                  padding: '0.3rem 0.6rem',
                  borderRadius: '6px',
                  backgroundColor: isDropdownOpen 
                    ? 'rgba(255, 255, 255, 0.9)'
                    : 'rgba(255, 255, 255, 0.12)',
                  transition: 'background-color 0.2s ease'
                }}
              >
                <span style={{
                  color: isDropdownOpen ? '#000000' : 'white',
                  textShadow: isDropdownOpen 
                    ? 'none' 
                    : '0 1px 2px rgba(0,0,0,0.25)',
                  fontWeight: '500',
                  transition: 'color 0.2s, text-shadow 0.2s'
                }}>
                  {user.login}
                </span>
                {smps.length > 0 && (
                  <svg 
                    width="12" 
                    height="12" 
                    viewBox="0 0 24 24" 
                    fill="none" 
                    stroke={isDropdownOpen ? '#000000' : 'white'}
                    strokeWidth="2"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    style={{
                      transform: isDropdownOpen ? 'rotate(180deg)' : 'rotate(0)',
                      transition: 'transform 0.2s ease, stroke 0.2s'
                    }}
                  >
                    <polyline points="6 9 12 15 18 9"></polyline>
                  </svg>
                )}
              </div>
              
              {isDropdownOpen && smps.length > 0 && (
                <div 
                  style={{
                    position: 'absolute',
                    top: '100%',
                    right: 0,
                    backgroundColor: 'white',
                    borderRadius: '4px',
                    boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)',
                    minWidth: '280px',
                    maxWidth: '90vw',
                    zIndex: 1000,
                    marginTop: '0.3rem',
                    overflow: 'hidden',
                  }}
                >
                  <div style={{ 
                    padding: '0.5rem 1rem',
                    backgroundColor: '#f8f9fa',
                    borderBottom: '1px solid #e9ecef',
                    fontWeight: '500',
                    fontSize: '0.8rem',
                    color: '#495057',
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center'
                  }}>
                    <span>Доступные СМП</span>
                    <span style={{ 
                      fontSize: '0.75rem',
                      color: '#6c757d',
                      backgroundColor: '#e9ecef',
                      padding: '0.1rem 0.4rem',
                      borderRadius: '10px',
                      fontWeight: 'normal'
                    }}>
                      {smps.length}
                    </span>
                  </div>
                  <div style={{ 
                    maxHeight: '400px',
                    overflowY: 'auto',
                    fontSize: '0.8rem',
                  }}>
                    {smps.map((smp, index) => (
                      <div 
                        key={`${smp.region_code}-${smp.smp_code}-${index}`}
                        style={{
                          padding: '0.3rem 0.8rem',
                          color: '#212529',
                          borderBottom: '1px solid #f1f3f5',
                          cursor: 'default',
                          whiteSpace: 'nowrap',
                          overflow: 'hidden',
                          textOverflow: 'ellipsis',
                          ':hover': {
                            backgroundColor: '#f8f9fa',
                          },
                        }}
                      >
                        <span style={{ 
                          display: 'inline-block',
                          minWidth: '60px',
                          color: '#6c757d',
                          marginRight: '0.5rem'
                        }}>
                          {smp.region_code}:
                        </span>
                        {smp.smp_code}
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}
          <button
            onClick={handleLogout}
            style={{
              padding: '0.4rem 0.9rem',
              backgroundColor: '#dc3545',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: 'pointer',
              transition: 'all 0.2s ease',
              fontSize: '0.9rem',
              fontWeight: '500',
              display: 'flex',
              alignItems: 'center',
              gap: '0.5rem'
            }}
            onMouseOver={(e) => e.target.style.backgroundColor = '#c82333'}
            onMouseOut={(e) => e.target.style.backgroundColor = '#dc3545'}
          >
            <span>Выйти</span>
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path>
              <polyline points="16 17 21 12 16 7"></polyline>
              <line x1="21" y1="12" x2="9" y2="12"></line>
            </svg>
          </button>
        </div>
      </header>
      <main style={{ 
        flex: 1, 
        padding: '1.5rem',
        maxWidth: '1400px',
        width: '100%',
        margin: '0 auto',
        boxSizing: 'border-box'
      }}>
        <Outlet context={{ user }} />
      </main>
    </div>
  );
}

export default Layout;
