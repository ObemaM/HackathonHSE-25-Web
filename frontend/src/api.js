const API_BASE = '/api'; // Адрес бэка, куда отправится HTTP-запрос

// Функция для выполнения запросов с авторизацией
async function fetchWithAuth(url, options = {}) {
    const response = await fetch(url, {
        ...options,
        credentials: 'include',
        headers: {
            'Content-Type': 'application/json',
            ...options.headers,
        },
    });

    if (response.status === 401 && !window.location.pathname.includes('/login')) {
        window.location.href = '/login';
        throw new Error('Требуется авторизация');
    }

    if (!response.ok) {
        const text = await response.text();
        throw new Error(`Ошибка (${response.status}): ${text || 'описание ошибки отсутствует.'}`);
    }

    return response;
}

// HTTP-запрос для входа администратора
export async function login(adminLogin, password) {
    const response = await fetchWithAuth(`${API_BASE}/login`, {
        method: 'POST',
        body: JSON.stringify({ login: adminLogin, password })
    });
    return true;
}

// HTTP-запрос для выхода из системы
export async function logout() {
    await fetchWithAuth(`${API_BASE}/logout`, {
        method: 'POST',
    });
}

// Получение данных текущего авторизованного пользователя
export async function getCurrentUser() {
    const response = await fetchWithAuth(`${API_BASE}/me`);
    return response.json();
}

// HTTP-запрос для получения последнего лога каждого из устройств в БД с пагинацией и фильтрами
export async function fetchLatestLogs(offset = 0, limit = 50, filters = {}) {
    const params = new URLSearchParams();
    params.append('offset', offset);
    params.append('limit', limit);
    
    // Добавляем фильтры в параметры запроса
    Object.entries(filters).forEach(([key, values]) => {
        if (values && values.length > 0) {
            values.forEach(value => {
                params.append(key, value);
            });
        }
    });
    
    const response = await fetchWithAuth(`${API_BASE}/logs/latest?${params.toString()}`);
    if (!response.ok) throw new Error('Не удалось загрузить последние логи устройств');
    return await response.json();
}

// HTTP-запрос для получения всех логов конкретного устройства в БД с пагинацией и фильтрами
export async function fetchDeviceLogs(deviceCode, offset = 0, limit = 50, filters = {}) {
    const params = new URLSearchParams();
    params.append('offset', offset);
    params.append('limit', limit);
    
    // Добавляем фильтры в параметры запроса
    Object.entries(filters).forEach(([key, values]) => {
        if (values && values.length > 0) {
            values.forEach(value => {
                params.append(key, value);
            });
        }
    });

    const url = `${API_BASE}/logs/device/${deviceCode}?${params.toString()}`;
    const response = await fetchWithAuth(url);
    if (!response.ok) throw new Error(`Не удалось загрузить логи устройства с кодом ${deviceCode}`);
    return await response.json();
}

// HTTP-запрос для получения уникальных значений для DevicesList с учетом текущих фильтров
export async function fetchUniqueValues(filters = {}) {
    const params = new URLSearchParams();
    
    // Добавляем параметры фильтров в запрос
    for (const [key, values] of Object.entries(filters)) {
        if (values && values.length > 0) {
            values.forEach(value => params.append(key, value));
        }
    }
    
    const queryString = params.toString();
    const url = queryString 
        ? `${API_BASE}/logs/unique-values?${queryString}` 
        : `${API_BASE}/logs/unique-values`;
    
    const response = await fetchWithAuth(url);
    if (!response.ok) throw new Error('Не удалось загрузить фильтры');
    return await response.json();
}

// HTTP-запрос для получения уникальных значений для DeviceLogs с учетом фильтров
export async function fetchUniqueValuesForDevice(deviceCode, filters = {}) {
    const params = new URLSearchParams();
    
    // Добавляем параметры фильтров в запрос
    for (const [key, values] of Object.entries(filters)) {
        if (values && values.length > 0) {
            values.forEach(value => params.append(key, value));
        }
    }
    
    const queryString = params.toString();
    const url = queryString 
        ? `${API_BASE}/logs/device/${deviceCode}/unique-values?${queryString}` 
        : `${API_BASE}/logs/device/${deviceCode}/unique-values`;
    
    const response = await fetchWithAuth(url);
    if (!response.ok) throw new Error('Не удалось загрузить фильтры для устройства');
    return await response.json();
}

// Получение списка устройств
export async function fetchDevices() {
    const response = await fetchWithAuth(`${API_BASE}/devices`);
    return response.json();
}

// Получение списка действий
export async function fetchActions() {
    const response = await fetchWithAuth(`${API_BASE}/actions`);
    return response.json();
}

// Получение списка СМП
export async function fetchSMP() {
    const response = await fetchWithAuth(`${API_BASE}/smp`);
    return response.json();
}

// Получение списка администраторов
export async function fetchAdmins() {
    const response = await fetchWithAuth(`${API_BASE}/admins`);
    return response.json();
}

// Получение связей администраторов и СМП
export async function fetchAdminSMP() {
    const response = await fetchWithAuth(`${API_BASE}/admins-smp`);
    return response.json();
}

// Получение СМП текущего пользователя
export async function fetchCurrentUserSMPs() {
    const response = await fetchWithAuth(`${API_BASE}/me/smps`);
    return response.json();
}
