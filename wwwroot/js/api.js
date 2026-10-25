async function api(path, options = {}) {
    const res = await fetch('/api' + path, {
        headers: { 'Content-Type': 'application/json' },
        ...options
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) {
        const err = new Error(data.error || 'Something went wrong.');
        err.status = res.status;
        err.data = data;
        throw err;
    }
    return data;
}

const JM = {
    me:          ()               => api('/me'),
    shop:        ()               => api('/shop'),
    buy:         (code, packSize) => api('/shop/buy', { method: 'POST', body: JSON.stringify({ code, packSize }) }),
    daily:       ()               => api('/daily'),
    dailyAnswer: (answer)         => api('/daily/answer', { method: 'POST', body: JSON.stringify({ answer }) }),
    repair:      ()               => api('/streak/repair', { method: 'POST' })
};