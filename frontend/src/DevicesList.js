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
    const navigate = useNavigate(); // Объект Для навигации по страницам сайта

    // Обновление уникальных значений при обновлении данных на устройствах
    useEffect(() => {
        if (devices.length > 0) {
            const values = {};
            fields.forEach(field => {
                values[field.key] = getUniqueValues(field.key);
            });
            setUniqueValues(values);
        }
    }, [devices]);

    // Выполняется при загрузке компонента и при любом изменении фильтров
    useEffect(() => {
        const load = async () => {
            setLoading(true); // Включает индикатор/состояние загрузки
            try {
                // Преобразует выбранные фильтры в формат для отправки на бэк: массив - строка через запятую
                const filtersToSend = {};
                for (const [key, value] of Object.entries(filters)) {
                    if (value && value.length > 0) {
                        filtersToSend[key] = value.join(','); // Например: smp_code=SMP-01,SMP-02
                    }
                }
                const data = await fetchLatestLogs(filtersToSend); // Вызов HTTP запроса из api.js с фильтрами
                setDevices(data); // Обновление таблицу
            } 
            catch (err) { // Если произошла ошибка, будет показано её сообщение
                setError(err.message);
            } 
            finally { // Блок происходит независимо оттого произошла ошибка или нет
                setLoading(false); // Считается, что загрузка завершена
            }
        };

        // Задержка (debounce), чтобы не отправлять запрос при каждом нажатии клавиши
        const timer = setTimeout(load, 300);
        return () => clearTimeout(timer); // Отмена таймера, если фильтр изменился снова
    }, [filters]);


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

    // Получение уникальных значений для каждого поля текущей таблицы
    const getUniqueValues = (key) => {
        const values = new Set();
        devices.forEach(device => {
            if (device[key]) {
                values.add(device[key].toString());
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

    // Функция для применения фильтров к данным
    const filterDevices = (devices, filters) => {
        // Если нет активных фильтров - возвращение всех устройств
        const activeFilters = Object.entries(filters).filter(([_, values]) => values && values.length > 0);
        if (activeFilters.length === 0) return devices;
        
        return devices.filter(device => {
            return activeFilters.every(([key, values]) => {
                const deviceValue = device[key] || '';
                return values.includes(deviceValue.toString());
            });
        });
    };
    
    // Применение фильтров к данным
    const filteredDevices = filterDevices(devices, filters);

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
                Всего устройств: <strong>{devices.length}</strong>
                {filteredDevices.length !== devices.length && (
                    <span style={{ marginLeft: '20px' }}>
                        Отфильтровано: <strong>{filteredDevices.length}</strong>
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
                                <th style={{ textAlign: 'center' }}>Дата действия</th>
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