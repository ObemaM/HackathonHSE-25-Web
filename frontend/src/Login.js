import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { login, getCurrentUser } from './api';

export default function Login() {
    const [loginValue, setLogin] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(true);
    const navigate = useNavigate();

    // Проверяем, авторизован ли пользователь
    useEffect(() => {
        const checkAuth = async () => {
            try {
                await getCurrentUser();
                // Если пользователь уже авторизован, перенаправляем на главную
                navigate('/devices');
            } catch (err) {
                // Если не авторизован, остаемся на странице входа
                setIsLoading(false);
            }
        };

        checkAuth();
    }, [navigate]);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError('');
        
        if (!loginValue || !password) {
            setError('Пожалуйста, введите логин и пароль');
            return;
        }

        try {
            await login(loginValue, password);
            navigate('/devices');
        } catch (err) {
            setError('Неверный логин или пароль');
            console.error('Login error:', err);
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
                <div>Проверка аутентификации...</div>
            </div>
        );
    }

    return (
        <div style={{ 
            maxWidth: '400px', 
            margin: '100px auto', 
            padding: '20px',
            boxShadow: '0 0 10px rgba(0,0,0,0.1)',
            borderRadius: '8px'
        }}>
            <h2 style={{ textAlign: 'center', marginBottom: '20px' }}>Вход в систему</h2>
            
            {error && (
                <div style={{ 
                    color: 'white', 
                    backgroundColor: '#ff4444',
                    padding: '10px',
                    borderRadius: '4px',
                    marginBottom: '20px',
                    textAlign: 'center'
                }}>
                    {error}
                </div>
            )}
            
            <form onSubmit={handleSubmit}>
                <div style={{ marginBottom: '15px' }}>
                    <label style={{ display: 'block', marginBottom: '5px', fontWeight: 'bold' }}>Логин</label>
                    <input
                        type="text"
                        value={loginValue}
                        onChange={(e) => setLogin(e.target.value)}
                        style={{ 
                            width: '100%', 
                            padding: '10px', 
                            border: '1px solid #ddd',
                            borderRadius: '4px',
                            boxSizing: 'border-box'
                        }}
                        placeholder="Введите логин"
                        disabled={isLoading}
                    />
                </div>
                
                <div style={{ marginBottom: '20px' }}>
                    <label style={{ display: 'block', marginBottom: '5px', fontWeight: 'bold' }}>Пароль</label>
                    <input
                        type="password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        style={{ 
                            width: '100%', 
                            padding: '10px', 
                            border: '1px solid #ddd',
                            borderRadius: '4px',
                            boxSizing: 'border-box'
                        }}
                        placeholder="Введите пароль"
                        disabled={isLoading}
                    />
                </div>
                
                <button
                    type="submit"
                    disabled={isLoading}
                    style={{
                        width: '100%',
                        padding: '12px',
                        backgroundColor: '#4CAF50',
                        color: 'white',
                        border: 'none',
                        borderRadius: '4px',
                        cursor: 'pointer',
                        fontSize: '16px',
                        transition: 'background-color 0.3s',
                        opacity: isLoading ? 0.7 : 1
                    }}
                    onMouseOver={e => !isLoading && (e.target.style.backgroundColor = '#45a049')}
                    onMouseOut={e => !isLoading && (e.target.style.backgroundColor = '#4CAF50')}
                >
                    {isLoading ? 'Вход...' : 'Войти'}
                </button>
            </form>
        </div>
    );
}
