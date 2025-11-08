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

export async function login(adminLogin, password) {
    const response = await fetchWithAuth(`${API_BASE}/login`, {
        method: 'POST',
        body: JSON.stringify({ login: adminLogin, password })
    });
    return true;
}

export async function logout() {
    await fetchWithAuth(`${API_BASE}/logout`, {
        method: 'POST',
    });
}

export async function getCurrentUser() {
    const response = await fetchWithAuth(`${API_BASE}/me`);
    return response.json();
}

// HTTP-запрос для получения последнего лога каждого из устройств в БД
export async function fetchLatestLogs() {
    const response = await fetchWithAuth(`${API_BASE}/logs/latest`);
    if (!response.ok) throw new Error('Не удалось загрузить последние логи устройств');
    return await response.json();
}

// HTTP-запрос для получения всех логов конкретного устройства в БД
export async function fetchDeviceLogs(deviceCode, filters = {}) {
    const queryParams = new URLSearchParams(filters).toString();
    const url = queryParams 
        ? `${API_BASE}/logs/device/${deviceCode}?${queryParams}` 
        : `${API_BASE}/logs/device/${deviceCode}`;

    const response = await fetchWithAuth(url);
    if (!response.ok) throw new Error(`Не удалось загрузить логи устройства с кодом ${deviceCode}`);
    return await response.json();
}

// HTTP-запрос для получения уникальных значений для DevicesList
export async function fetchUniqueValues() {
    const response = await fetchWithAuth(`${API_BASE}/logs/unique-values`);
    if (!response.ok) throw new Error('Не удалось загрузить фильтры');
    return await response.json();
}

// HTTP-запрос для получения уникальных значений для DeviceLogs
export async function fetchUniqueValuesForDevice(deviceCode) {
    const response = await fetchWithAuth(`${API_BASE}/logs/device/${deviceCode}/unique-values`);
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
