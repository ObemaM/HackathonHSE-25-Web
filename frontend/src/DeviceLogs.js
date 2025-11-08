import { useState, useEffect } from 'react'; // useState для хранения состояния (как в Login.js), useEffect - выполняет код при загрузке компонента
import { useNavigate, useParams } from 'react-router-dom'; // для перехода на другую страницу
import { fetchDeviceLogs, fetchUniqueValuesForDevice } from './api'; // функции из api.js
import './index.css';


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
    const [offset, setOffset] = useState(0); // Смещение для пагинации
    const [hasMore, setHasMore] = useState(true); // Есть ли еще данные для загрузки
    const [totalCount, setTotalCount] = useState(0); // Общее количество логов после фильтрации
    const [totalUnfiltered, setTotalUnfiltered] = useState(0); // Общее количество логов БЕЗ фильтров
    const [isLoadingMore, setIsLoadingMore] = useState(false); // Идет ли подгрузка следующей порции
    const navigate = useNavigate(); // Объект Для навигации по страницам сайта

    // Описание полей таблицы и их названий для фильтрации
    const fields = [
        { key: 'region_code', label: 'Регион' },
        { key: 'smp_code', label: 'СМП' },
        { key: 'team_number', label: 'Команда' },
        { key: 'action_text', label: 'Действие' },
        { key: 'app_version', label: 'Версия приложения' }
    ];

    // Загрузка уникальных значений для фильтров с учетом текущих фильтров
    useEffect(() => {
        const loadUniqueValues = async () => {
            try {
                const values = await fetchUniqueValuesForDevice(deviceCode, filters); // Передаем текущие фильтры
                setUniqueValues(values);
            } catch (err) {
                console.error('Ошибка загрузки уникальных значений:', err);
            }
        };
        loadUniqueValues();
    }, [deviceCode, filters]); // Перезагружаем при изменении deviceCode или фильтров

    // Загрузка логов при монтировании компонента и при изменении фильтров
    useEffect(() => {
        const load = async () => {
            setLoading(true);
            setOffset(0); // Сбрасываем offset при изменении фильтров
            try {
                const response = await fetchDeviceLogs(deviceCode, 0, 50, filters); // Загружаем первую порцию
                setLogs(response.data); // Устанавливаем новые данные
                setTotalCount(response.total); // Общее количество после фильтрации
                setTotalUnfiltered(response.totalUnfiltered); // Общее количество БЕЗ фильтров
                setHasMore(response.hasMore); // Есть ли еще данные
                setOffset(50); // Следующая загрузка начнется с 50-го элемента
            } catch (err) {
                setError(err.message);
            } finally {
                setLoading(false);
            }
        };
        load();
    }, [deviceCode, filters]);

    // Функция для загрузки следующей порции логов при прокрутке
    const loadMore = async () => {
        if (isLoadingMore || !hasMore) return; // Если уже идет загрузка или нет больше данных - выходим
        
        setIsLoadingMore(true); // Начинаем загрузку
        try {
            const response = await fetchDeviceLogs(deviceCode, offset, 50, filters); // Загружаем следующую порцию
            setLogs(prev => [...prev, ...response.data]); // Добавляем новые данные к существующим
            setTotalCount(response.total); // Обновляем общее количество после фильтрации
            setTotalUnfiltered(response.totalUnfiltered); // Обновляем общее количество БЕЗ фильтров
            setHasMore(response.hasMore); // Обновляем флаг hasMore
            setOffset(prev => prev + 50); // Увеличиваем offset на 50
        } catch (err) {
            setError(err.message);
        } finally {
            setIsLoadingMore(false); // Завершаем загрузку
        }
    };

    // Обработчик прокрутки для infinite scroll
    useEffect(() => {
        const handleScroll = () => {
            // Проверяем достиг ли пользователь конца страницы (с запасом 100px)
            const scrolledToBottom = window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 100;
            
            if (scrolledToBottom && hasMore && !isLoadingMore) {
                loadMore(); // Загружаем следующую порцию
            }
        };

        window.addEventListener('scroll', handleScroll); // Подписываемся на событие прокрутки
        return () => window.removeEventListener('scroll', handleScroll); // Отписываемся при размонтировании
    }, [hasMore, isLoadingMore, offset, filters]); // Зависимости для useEffect

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

    // Получение уникальных значений для каждого поля из данных с сервера
    const getUniqueValues = (key) => {
        return uniqueValues[key] || []; // Возвращаем значения с сервера
    };

    // Фильтрация значений по поисковому запросу
    const getFilteredValues = (key) => {
        const searchTerm = (searchTerms[key] || '').toLowerCase();
        const values = getUniqueValues(key);
        return values.filter(value => 
            value.toString().toLowerCase().includes(searchTerm)
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

    // Фильтрация теперь происходит на backend, поэтому используем logs напрямую
    const filteredLogs = logs;

    return (
        <div style={{ padding: '20px' }}>
            <button 
                onClick={() => navigate('/devices')}
                style={{
                    padding: '0.5rem 1rem',
                    backgroundColor: '#2D266C',
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
            <p style={{ color: '#333', marginBottom: '1.5rem' }}>
                Всего записей: <strong>{totalUnfiltered}</strong>
                {totalCount !== totalUnfiltered && (
                    <span style={{ marginLeft: '20px' }}>
                        Отфильтровано: <strong>{totalCount}</strong>
                    </span>
                )}
            </p>

            

            {/* Таблица логов */}
            <div style={{ background: 'white', borderRadius: '12px', boxShadow: '0 4px 12px rgba(0,0,0,0.08)', overflow: 'visible', position: 'relative' }}>
                <div style={{ overflowX: 'auto' , overflow: 'visible'}}>
                    <table style={{ 
                        width: '100%', 
                        borderCollapse: 'collapse',
                        minWidth: '800px',
                        textAlign: 'center'
                    }}>
                        <thead>
                            {/*Строка фильтров */}
                            <tr style={{ 
                                background: '#f0f2f8',
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
                                    borderRight: '1px solid #e9ecef',
                                    borderTopLeftRadius: '12px'
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
                                        color: filters[field.key]?.length > 0 ? '#372F85' : '#6c757d',
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
                                            minWidth: '200px',
                                            borderRadius: '4px',
                                            width: 'max-content',
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
                                                fontSize: '13px',
                                                boxSizing: 'border-box'
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
                                <th style={{ 
                                    textAlign: 'center',
                                    padding: '0.5rem',
                                    borderTopRightRadius: '12px'}}></th>
                            </tr>

                            {/*Строка заголовков */}
                            <tr style={{ 
                                backgroundColor: '#FFFFFF',
                                borderBottom: '2px solid #372F85'
                            }}>
                                {fields.map(f => (
                                <th key={f.key} style={{ 
                                    textAlign: 'center',
                                    verticalAlign: 'middle',
                                    borderRight: '1px solid #dee2e6'
                                }}>
                                    {f.label}
                                </th>
                                ))}
                                <th style={{ 
                                textAlign: 'center', 
                                verticalAlign: 'middle'
                                }}>Дата действия</th>
                            </tr>

                        </thead>
                        <tbody>
                            {filteredLogs.length === 0 ? (
                                <tr>
                                    <td colSpan={fields.length + 1} style={{ padding: '2rem', textAlign: 'center', color: '#333' }}>
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
                                        }}>
                                            {log.region_code || '—'}
                                        </td>
                                        <td style={{ 
                                            padding: '1rem',
                                            borderRight: '1px solid #eee',
                                            textAlign: 'center',
                                            verticalAlign: 'middle',
                                            color: '#333'
                                        }}>
                                            {log.smp_code || '—'}
                                        </td>
                                        <td style={{ 
                                            padding: '1rem',
                                            borderRight: '1px solid #eee',
                                            textAlign: 'center',
                                            verticalAlign: 'middle',
                                            color: '#333'
                                        }}>
                                            {log.team_number || '—'}
                                        </td>
                                        <td style={{ 
                                            padding: '1rem',
                                            borderRight: '1px solid #eee',
                                            textAlign: 'center',
                                            verticalAlign: 'middle',
                                            color: '#333'
                                        }}>
                                            {log.action_text || '—'}
                                        </td>
                                        <td style={{ 
                                            padding: '1rem',
                                            borderRight: '1px solid #eee',
                                            textAlign: 'center',
                                            verticalAlign: 'middle',
                                            color: '#333',
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
                {/* Благодаря этому блоку не торчит линия-разделитель строк таблицы в последней ячейке */}
                <div style={{
                    position: 'absolute',
                    bottom: '-14px',
                    left: 0,
                    right: 0,
                    height: '25px',
                    background: 'white',
                    borderBottomLeftRadius: '12px',
                    borderBottomRightRadius: '12px',
                    zIndex: 1
                }} />
            </div>

            {/* Индикатор загрузки данных, не замтено пока, но при больших объемах возможно */}
            {isLoadingMore && (
                <div style={{ 
                    textAlign: 'center', 
                    padding: '1rem', 
                    color: '#666',
                    fontSize: '0.9rem'
                }}>
                    Загрузка данных...
                </div>
            )}

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