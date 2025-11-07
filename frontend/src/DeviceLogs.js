import { useState, useEffect } from 'react'; // useState для хранения состояния (как в Login.js), useEffect - выполняет код при загрузке компонента
import { useNavigate, useParams } from 'react-router-dom'; // для перехода на другую страницу
import { fetchDeviceLogs } from './api'; // функции из api.js


function formatDate(dateString) {
    // Принимает дату
    // Возвращает дату в привычном для России виде
    return new Date(dateString).toLocaleString('ru-RU');
}

export default function DeviceLogs() {
    // Привязка данных к интерфейсу
    const { deviceCode } = useParams();
    const [logs, setLogs] = useState([]); // Массив логов (действий) для конкретного устройства - то что вернёт бэкенд. Изначально - пустой массив
    const [loading, setLoading] = useState(true); // Идёт ли сечас загрузка? Изначально true (идёт)
    const [error, setError] = useState(null); // Ошибка во время работы программы
    const [filters, setFilters] = useState({}); // Выбранные значения фильтров
    const [uniqueValues, setUniqueValues] = useState({}); // Все уникальные значения для фильтров
    const [openFilter, setOpenFilter] = useState(null); // Какая выпадающая панель открыта
    const [searchTerms, setSearchTerms] = useState({}); // Поисковые запросы для фильтров
    const navigate = useNavigate(); // Объект Для навигации по страницам сайта

    // Описание полей таблицы и их названий для фильтрации
    const fields = [
        { key: 'region_code', label: 'Регион' },
        { key: 'smp_code', label: 'СМП' },
        { key: 'team_number', label: 'Команда' },
        { key: 'action_text', label: 'Действие' },
        { key: 'app_version', label: 'Версия приложения' }
    ];

    // Обновление уникальных значений при изменении даты логов
    useEffect(() => {
        if (logs.length > 0) {
            const values = {};
            fields.forEach(field => {
                values[field.key] = getUniqueValues(field.key);
            });
            setUniqueValues(values);
        }
    }, [logs]);

    // Загрузка логов один раз при монтировании компонента
    useEffect(() => {
        const load = async () => {
            setLoading(true);
            try {
                const data = await fetchDeviceLogs(deviceCode, {});
                setLogs(data);
            } catch (err) {
                setError(err.message);
            } finally {
                setLoading(false);
            }
        };
        load();
    }, [deviceCode]);

    const toggleFilter = (field, value) => {
        setFilters(prev => {
            const current = prev[field] || [];
            const newValues = current.includes(value)
                ? current.filter(v => v !== value)
                : [...current, value];
            return { ...prev, [field]: newValues };
        });
    };

    const clearFilter = (field) => {
        setFilters(prev => {
            const newFilters = { ...prev };
            delete newFilters[field];
            return newFilters;
        });
    };

    // Получение уникальных значений для каждого поля из текущей таблицы
    const getUniqueValues = (key) => {
        const values = new Set();
        logs.forEach(log => {
            if (log[key]) {
                values.add(log[key].toString());
            }
        });
        return Array.from(values).sort();
    };

    // Фильтрация значений по поисковому запросу
    const getFilteredValues = (key) => {
        const searchTerm = (searchTerms[key] || '').toLowerCase();
        return getUniqueValues(key).filter(value => 
            value.toLowerCase().includes(searchTerm)
        );
    };

    const handleSearchChange = (field, value) => {
        setSearchTerms(prev => ({
            ...prev,
            [field]: value
        }));
    };

    if (loading && logs.length === 0) return <div style={{ padding: '20px', textAlign: 'center', marginTop: '2rem' }}>Загрузка данных...</div>;
    if (error) return <div style={{ color: '#dc3545', padding: '20px', background: '#fff8f8', borderRadius: '4px', margin: '1rem 0' }}>Ошибка загрузки: {error}</div>;

    // Функция для применения фильтров к данным
    const filterLogs = (logs, filters) => {
        // Если нет активных фильтров - возврат всех логов
        const activeFilters = Object.entries(filters).filter(([_, values]) => values && values.length > 0);
        if (activeFilters.length === 0) return logs;
        
        return logs.filter(log => {
            return activeFilters.every(([key, values]) => {
                const logValue = log[key] || '';
                return values.includes(logValue.toString());
            });
        });
    };
    
    // Применяем фильтры к данным
    const filteredLogs = filterLogs(logs, filters);

    return (
        <div style={{ padding: '20px' }}>
            <button 
                onClick={() => navigate('/devices')}
                style={{
                    padding: '0.5rem 1rem',
                    backgroundColor: '#6c757d',
                    color: 'white',
                    border: 'none',
                    borderRadius: '4px',
                    cursor: 'pointer',
                    fontSize: '0.9rem',
                    marginBottom: '1rem'
                }}
            >
                &larr; Назад к списку
            </button>
            <h2 style={{ margin: '0 0 1rem 0', fontSize: '1.5rem', color: '#333' }}>Логи устройства: {deviceCode}</h2>
            <p style={{ color: '#666', marginBottom: '1.5rem' }}>
                Всего записей: <strong>{logs.length}</strong>
                {filteredLogs.length !== logs.length && (
                    <span style={{ marginLeft: '20px' }}>
                        Отфильтровано: <strong>{filteredLogs.length}</strong>
                    </span>
                )}
            </p>

            

            {/* Таблица логов */}
            <div style={{ background: 'white', borderRadius: '8px', boxShadow: '0 2px 8px rgba(0,0,0,0.1)', overflow: 'hidden' }}>
                <div style={{ overflowX: 'auto' }}>
                    <table style={{ 
                        width: '100%', 
                        borderCollapse: 'collapse',
                        minWidth: '800px',
                        textAlign: 'center'
                    }}>
                        <thead>
                            {/* 🔹 Строка фильтров */}
                            <tr style={{ 
                                backgroundColor: '#f1f3f5',
                                textAlign: 'center',
                                verticalAlign: 'middle',
                                borderBottom: '1px solid #e9ecef'
                                
                            }}>
                                {fields.map(field => (
                                <th 
                                    key={`filter-${field.key}`} 
                                    style={{ 
                                    textAlign: 'center',
                                    verticalAlign: 'middle',
                                    padding: '0.5rem',
                                    borderRight: '1px solid #e9ecef'
                                    }}
                                >
                                    <div style={{ position: 'relative', display: 'inline-block' }}>
                                    <button
                                        onClick={() => setOpenFilter(openFilter === field.key ? null : field.key)}
                                        style={{
                                        padding: '6px 12px',
                                        border: '1px solid #ced4da',
                                        background: filters[field.key]?.length > 0 ? '#e0f0ff' : '#f8f9fa',
                                        cursor: 'pointer',
                                        borderRadius: '4px',
                                        fontSize: '0.875rem',
                                        color: filters[field.key]?.length > 0 ? '#1976d2' : '#6c757d',
                                        transition: 'all 0.2s'
                                        }}
                                    >
                                        {filters[field.key]?.length > 0 ? `(${filters[field.key].length})` : 'Фильтр'}
                                    </button>

                                    {openFilter === field.key && (
                                        <div
                                        style={{
                                            position: 'absolute',
                                            top: '100%',
                                            left: 0,
                                            background: 'white',
                                            border: '1px solid #ced4da',
                                            boxShadow: '0 3px 8px rgba(0,0,0,0.15)',
                                            zIndex: 1000,
                                            maxHeight: '200px',
                                            overflowY: 'auto',
                                            minWidth: '160px',
                                            borderRadius: '4px',
                                            marginTop: '4px'
                                        }}
                                        onClick={e => e.stopPropagation()}
                                        >
                                        <div style={{ padding: '6px', borderBottom: '1px solid #eee' }}>
                                            <button
                                            onClick={() => clearFilter(field.key)}
                                            style={{ fontSize: '12px', color: '#1976d2', background: 'none', border: 'none', cursor: 'pointer' }}
                                            >
                                            Очистить
                                            </button>
                                        </div>
                                        <div style={{ padding: '8px', borderBottom: '1px solid #eee' }}>
                                            <input
                                            type="text"
                                            placeholder={`Поиск...`}
                                            value={searchTerms[field.key] || ''}
                                            onChange={(e) => handleSearchChange(field.key, e.target.value)}
                                            style={{
                                                width: '100%',
                                                padding: '4px 8px',
                                                borderRadius: '4px',
                                                border: '1px solid #ddd',
                                                fontSize: '13px'
                                            }}
                                            />
                                        </div>
                                        {getFilteredValues(field.key).map(value => (
                                            <label key={value} style={{ display: 'flex', alignItems: 'center', padding: '6px 10px', cursor: 'pointer' }}>
                                            <input
                                                type="checkbox"
                                                checked={filters[field.key]?.includes(value) || false}
                                                onChange={() => toggleFilter(field.key, value)}
                                            />
                                            <span style={{ marginLeft: '8px', fontSize: '0.9rem' }}>{value || '—'}</span>
                                            </label>
                                        ))}
                                        </div>
                                    )}
                                    </div>
                                </th>
                                ))}

                                {/* Пустая ячейка под "Дата действия" */}
                                <th style={{ textAlign: 'center', padding: '0.5rem' }}></th>
                            </tr>

                            {/* 🔹 Строка заголовков */}
                            <tr style={{ 
                                backgroundColor: '#f8f9fa',
                                borderBottom: '2px solid #dee2e6'
                            }}>
                                {fields.map(f => (
                                <th key={f.key} style={{ 
                                    textAlign: 'center',
                                    verticalAlign: 'middle',
                                    padding: '0.75rem 1rem',
                                    borderRight: '1px solid #dee2e6',
                                    fontWeight: '600',
                                    color: '#495057'
                                }}>
                                    {f.label}
                                </th>
                                ))}
                                <th style={{ 
                                textAlign: 'center', 
                                verticalAlign: 'middle',
                                padding: '0.75rem 1rem',
                                fontWeight: '600',
                                color: '#495057'
                                }}>Дата действия</th>
                            </tr>

                        </thead>
                        <tbody>
                            {filteredLogs.length === 0 ? (
                                <tr>
                                    <td colSpan={fields.length + 1} style={{ padding: '2rem', textAlign: 'center', color: '#666' }}>
                                        Нет логов
                                    </td>
                                </tr>
                            ) : (
                                filteredLogs.map((log, index) => (
                                    <tr 
                                        key={index}
                                        style={{
                                            borderBottom: '1px solid #eee',
                                            transition: 'background-color 0.2s'
                                        }}
                                    >
                                        <td style={{ 
                                            padding: '1rem',
                                            textAlign: 'center',
                                            verticalAlign: 'middle',
                                            borderRight: '1px solid #eee',
                                            color: '#333',
                                            fontWeight: '500'
                                        }}>
                                            {log.region_code || '—'}
                                        </td>
                                        <td style={{ 
                                            padding: '1rem',
                                            borderRight: '1px solid #eee',
                                            textAlign: 'center',
                                            verticalAlign: 'middle',
                                            color: '#666'
                                        }}>
                                            {log.smp_code || '—'}
                                        </td>
                                        <td style={{ 
                                            padding: '1rem',
                                            borderRight: '1px solid #eee',
                                            textAlign: 'center',
                                            verticalAlign: 'middle',
                                            color: '#666'
                                        }}>
                                            {log.team_number || '—'}
                                        </td>
                                        <td style={{ 
                                            padding: '1rem',
                                            borderRight: '1px solid #eee',
                                            textAlign: 'center',
                                            verticalAlign: 'middle',
                                            color: '#666'
                                        }}>
                                            {log.action_text || '—'}
                                        </td>
                                        <td style={{ 
                                            padding: '1rem',
                                            borderRight: '1px solid #eee',
                                            textAlign: 'center',
                                            verticalAlign: 'middle',
                                            color: '#666',
                                            textAlign: 'center'
                                        }}>
                                            {log.app_version || '—'}
                                        </td>
                                        <td style={{ 
                                            padding: '1rem',
                                            textAlign: 'center',
                                            verticalAlign: 'middle'
                                        }}>
                                            {formatDate(log.datetime)}
                                        </td>
                                    </tr>
                                ))
                            )}
                        </tbody>
                    </table>
                </div>
            </div>

            {/* Оверлей для закрытия фильтров */}
            {openFilter && (
                <div
                    style={{
                        position: 'fixed',
                        top: 0,
                        left: 0,
                        right: 0,
                        bottom: 0,
                        zIndex: 999
                    }}
                    onClick={() => setOpenFilter(null)}
                />
            )}
        </div>
    );
}