import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { fetchLatestLogs, fetchUniqueValues } from './api';
import './index.css';

function formatDate(dateString) {
    // Принимает дату
    // Возвращает дату в привычном для России виде
    return new Date(dateString).toLocaleString('ru-RU');
}

export default function DevicesList() {
    // Привязка данных к интерфейсу
    const [devices, setDevices] = useState([]); // Массив устройств - то что вернёт бэкенд. Изначально - пустой массив
    const [loading, setLoading] = useState(true); // Идёт ли сечас загрузка? Изначально true (идёт)
    const [error, setError] = useState(null); // Ошибка во время работы программы
    const [filters, setFilters] = useState({}); // Выбранные значения фильтров: { smp_code: ['SMP-01'], region_code: ['77'] }
    const [uniqueValues, setUniqueValues] = useState({}); // Уникальные значения для фильтров
    const [openFilter, setOpenFilter] = useState(null); // Какая выпадающая панель фильтра сейчас открыта (null - ни одна)
    const [searchTerms, setSearchTerms] = useState({}); // Поисковые запросы для фильтров
    const [offset, setOffset] = useState(0); // Смещение для пагинации - сколько элементов уже загружено
    const [hasMore, setHasMore] = useState(true); // Есть ли еще данные для загрузки
    const [totalCount, setTotalCount] = useState(0); // Общее количество элементов после фильтрации
    const [totalUnfiltered, setTotalUnfiltered] = useState(0); // Общее количество элементов БЕЗ фильтров
    const [isLoadingMore, setIsLoadingMore] = useState(false); // Идет ли подгрузка следующей порции
    const navigate = useNavigate(); // Объект Для навигации по страницам сайта

    // Загрузка уникальных значений для фильтров с учетом текущих фильтров (каскадная фильтрация)
    useEffect(() => {
        const loadUniqueValues = async () => {
            try {
                const values = await fetchUniqueValues(filters); // Передаем текущие фильтры
                setUniqueValues(values);
            } catch (err) {
                console.error('Ошибка загрузки уникальных значений:', err);
            }
        };
        loadUniqueValues();
    }, [filters]); // Перезагружаем при изменении фильтров

    // Выполняется при загрузке компонента и при любом изменении фильтров для загрузки данных таблицы
    useEffect(() => {
        const load = async () => {
            setLoading(true); // Включает индикатор/состояние загрузки
            setOffset(0); // Сбрасываем offset при изменении фильтров
            try {
                const response = await fetchLatestLogs(0, 50, filters); // Загружаем первую порцию данных (offset=0, limit=50)
                setDevices(response.data); // Устанавливаем новые данные (заменяем старые)
                setTotalCount(response.total); // Общее количество после фильтрации
                setTotalUnfiltered(response.totalUnfiltered); // Общее количество БЕЗ фильтров
                setHasMore(response.hasMore); // Есть ли еще данные
                setOffset(50); // Следующая загрузка начнется с 50-го элемента
            } 
            catch (err) { // Если произошла ошибка, будет показано её сообщение
                setError(err.message);
            } 
            finally { // Блок происходит независимо оттого произошла ошибка или нет
                setLoading(false); // Считается, что загрузка завершена
            }
        };

        load(); // Вызываем сразу при изменении фильтров
    }, [filters]);

    // Функция для загрузки следующей порции данных при прокрутке вниз
    const loadMore = async () => {
        if (isLoadingMore || !hasMore) return; // Если уже идет загрузка или нет больше данных - выходим
        
        setIsLoadingMore(true); // Начинаем загрузку
        try {
            const response = await fetchLatestLogs(offset, 50, filters); // Загружаем следующую порцию
            setDevices(prev => [...prev, ...response.data]); // Добавляем новые данные к существующим
            setTotalCount(response.total); // Обновляем общее количество
            setTotalUnfiltered(response.totalUnfiltered); // Обновляем общее количество БЕЗ фильтров
            setHasMore(response.hasMore); // Обновляем флаг hasMore
            setOffset(prev => prev + 50); // Увеличиваем offset на 50
        } catch (err) {
            setError(err.message);
        } finally {
            setIsLoadingMore(false); // Завершаем загрузку
        }
    };

    // Обработчик прокрутки для infinite scroll - подгружает данные при приближении к концу списка
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

    // Обработчик изменения выбранного значения в фильтре
    const toggleFilter = (field, value) => {
        setFilters(prev => {
            const current = prev[field] || []; // Текущие выбранные значения для этого поля
            const newValues = current.includes(value)
                ? current.filter(v => v !== value) // Если значение уже выбрано - снимает галочку
                : [...current, value];             // Если нет - ставит галочку
            return { ...prev, [field]: newValues };
        });
    };

    // Очистка фильтра по конкретному полю
    const clearFilter = (field) => {
        setFilters(prev => {
            const newFilters = { ...prev };
            delete newFilters[field]; // Удаление поля из объекта фильтров
            return newFilters;
        });
    };

    // Описание полей таблицы и их названий для фильтрации
    const fields = [
        { key: 'region_code', label: 'Регион' },
        { key: 'smp_code', label: 'СМП' },
        { key: 'team_number', label: 'Команда' },
        { key: 'action_text', label: 'Последнее действие' },
        { key: 'app_version', label: 'Версия приложения' },
        { key: 'device_code', label: 'Номер устройства' }
    ];

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

    // Фильтрация теперь происходит на backend, поэтому используем devices напрямую
    const filteredDevices = devices;

    return (
        <div style={{ padding: '20px' }}>
            <div style={{
                minHeight: '48px', 
                display: 'flex',
                alignItems: 'flex-start'
            }}></div> {/* Резервирование места под кнопку на DeviceLogs - 
            чтобы не "прыгала" таблица */}
            <h2 style={{ margin: '0 0 1rem 0', fontSize: '1.5rem', color: '#333' }}>Последние логи устройств</h2>
            <p style={{ color: '#666', marginBottom: '1.5rem' }}>
                Всего устройств: <strong>{totalUnfiltered}</strong>
                {totalCount !== totalUnfiltered && (
                    <span style={{ marginLeft: '20px' }}>
                        Отфильтровано: <strong>{totalCount}</strong>
                    </span>
                )}
            </p>

            {/* Таблица устройств */}
            <div style={{ background: 'white', borderRadius: '12px', boxShadow: '0 4px 12px rgba(0,0,0,0.08)', overflow: 'visible', position: 'relative'}}>
                <div style={{ overflowX: 'auto', overflow: 'visible' }}>
                    <table style={{ 
                        width: '100%', 
                        borderCollapse: 'collapse',
                        minWidth: '800px',
                        textAlign: 'center'
                    }}>
                        <thead>
                            <tr style={{ 
                            background: '#f0f2f8',
                            borderBottom: '1px solid #e9ecef'
                        }}>
                            {fields.map(field => (
                                <th 
                                    key={`filter-${field.key}`} 
                                    style={{
                                        borderRight: '1px solid #e9ecef',
                                        padding: '0.5rem',
                                        textAlign: 'center',
                                        verticalAlign: 'middle',
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

                                        {/* Выпадающий список */}
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
                                                    borderRadius: '4px',
                                                    minWidth: '200px',
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

                            {/* Пустые ячейки под "Дата действия" и "Действия" */}
                            <th style={{ textAlign: 'center', padding: '0.5rem', borderRight: '1px solid #e9ecef' }}></th>
                            <th style={{ textAlign: 'center', padding: '0.5rem', borderTopRightRadius: '12px'}}></th>
                        </tr>

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
                                <th style={{ textAlign: 'center', borderRight: '1px solid #dee2e6' }}>Дата действия</th>
                                <th style={{ textAlign: 'center' }}>Действия</th>
                            </tr>
                        </thead>
                        <tbody>
                          {filteredDevices.length === 0 ? (
                            <tr>
                              <td colSpan={8} style={{ padding: '2rem', textAlign: 'center', color: '#6c757d' }}>
                                {loading ? 'Загрузка...' : error ? `Ошибка: ${error}` : 'Нет данных'}
                              </td>
                            </tr>
                          ) : (
                            filteredDevices.map((d, index) => (
                              <tr key={index} style={{ borderBottom: '1px solid #eee' }}>
                                <td style={{ padding: '1rem', textAlign: 'center', borderRight: '1px solid #eee' }}>{d.region_code || '-'}</td>
                                <td style={{ padding: '1rem', borderRight: '1px solid #eee' }}>{d.smp_code || '-'}</td>
                                <td style={{ padding: '1rem', borderRight: '1px solid #eee' }}>{d.team_number || '-'}</td>
                                <td style={{ padding: '1rem', borderRight: '1px solid #eee' }}>{d.action_text || '—'}</td>
                                <td style={{ padding: '1rem', textAlign: 'center', borderRight: '1px solid #eee' }}>{d.app_version || '—'}</td>
                                <td style={{ padding: '1rem', textAlign: 'center', borderRight: '1px solid #eee' }}>{d.device_code || '—'}</td>
                                <td style={{ padding: '1rem', textAlign: 'center', borderRight: '1px solid #eee' }}>{formatDate(d.datetime)}</td>
                                <td style={{ padding: '1rem', textAlign: 'center' }}>
                                  <button 
                                    onClick={() => navigate(`/device/${d.device_code}`)}
                                    style={{ padding: '0.4rem 1rem', backgroundColor: '#2D266C', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}
                                  >
                                    Подробнее
                                  </button>
                                </td>
                              </tr>
                            ))
                          )}
                        </tbody>
                    </table>
                </div>
                {/* Благодаря этому блоку не торчит линия-разделитель строк табЫлицы в последней ячейке */}
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

            {/* Прозрачный оверлей для закрытия выпадающих панелей при клике вне */}
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