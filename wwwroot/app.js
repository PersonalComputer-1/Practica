let token = localStorage.getItem('token') || '';
let userRole = localStorage.getItem('role') || '';

const api = (p, o = {}) => fetch('/api' + p, {
  ...o,
  headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token, ...o.headers }
}).then(async r => {
    if (!r.ok) throw new Error(await r.text() || 'Ошибка сервера');
    return r.status === 204 ? {} : r.json().catch(() => ({}));
});

function login() {
  api('/auth/login', { method: 'POST', body: JSON.stringify({
    email: document.getElementById('email').value,
    password: document.getElementById('pass').value
  })}).then(d => {
    token = d.token; userRole = d.role;
    localStorage.setItem('token', token); localStorage.setItem('role', userRole);
    alert('OK: ' + d.role); updateUI();
  }).catch(e => alert('Ошибка: ' + e.message));
}

function register() {
  api('/auth/register', { method: 'POST', body: JSON.stringify({
    email: document.getElementById('email').value,
    password: document.getElementById('pass').value,
    fullName: 'Новый клиент'
  })}).then(d => { 
    token = d.token; userRole = d.role;
    localStorage.setItem('token', token); localStorage.setItem('role', userRole); 
    updateUI(); 
  }).catch(e => alert(e.message));
}

function updateUI() {
  document.getElementById('adminPanel').style.display = (userRole === 'admin') ? 'block' : 'none';
  document.getElementById('managerPanel').style.display = (userRole === 'manager' || userRole === 'admin') ? 'block' : 'none';
  document.getElementById('lk').style.display = (token && userRole === 'client') ? 'block' : 'none';
  
  if (userRole === 'client') showLk();
  if (userRole === 'manager' || userRole === 'admin') loadAllBookings();
  loadCatalog();
}

function loadCatalog() {
  const q = document.getElementById('search').value;
  api('/equipment?q=' + encodeURIComponent(q)).then(list => {
    document.getElementById('eqList').innerHTML = list.map(e => `
      <div class="card">
        <h3>${e.name}</h3>
        <p>${e.description}</p>
        <div class="price">${e.pricePerDay} ₽ <span>/ сутки</span></div>
        <p>В наличии: <b>${e.totalQty} шт.</b></p>
        ${userRole === 'client' ? `
        <div class="booking-actions">
          <input type="date" id="s_${e.id}">
          <input type="date" id="e_${e.id}">
          <input type="number" id="q_${e.id}" value="1" min="1">
          <button class="btn-primary" onclick="book(${e.id})">Забронировать</button>
        </div>` : ''}
      </div>`).join('');
  }).catch(e => console.error('Ошибка загрузки каталога:', e));
}

function book(id) {
  const body = {
    equipmentId: id,
    qty: +document.getElementById('q_' + id).value,
    startDate: document.getElementById('s_' + id).value,
    endDate: document.getElementById('e_' + id).value
  };
  api('/bookings', { method: 'POST', body: JSON.stringify(body) })
    .then(d => { alert('Бронь #' + d.id + ' на ' + d.totalPrice + ' ₽'); showLk(); })
    .catch(e => alert('Ошибка: ' + e.message));
}

function showLk() {
  api('/bookings/my').then(list => {
    document.getElementById('myBookings').innerHTML = list.map(b => `
      <div class="booking-item">
        <div class="booking-info">
          <b>Заказ #${b.id}</b> · ${b.startDate.slice(0,10)} — ${b.endDate.slice(0,10)}<br>
          <small>${b.items.map(i => (i.equipment?.name || 'Товар') + ' × ' + i.qty).join(', ')}</small>
        </div>
        <div style="display:flex; align-items:center; gap: 12px;">
          <span class="booking-status status-${b.status}">${b.status}</span>
          <b>${b.totalPrice} ₽</b>
          ${b.status === 'pending' ? `<button class="btn-outline" onclick="cancel(${b.id})">Отменить</button>` : ''}
        </div>
      </div>`).join('');
  }).catch(() => {});
}

function cancel(id) {
  api('/bookings/' + id + '/cancel', { method: 'PATCH' }).then(showLk).catch(e => alert(e.message));
}

function addEquipment() {
  const body = {
    name: document.getElementById('newEqName').value,
    pricePerDay: +document.getElementById('newEqPrice').value,
    totalQty: +document.getElementById('newEqQty').value,
    description: document.getElementById('newEqDesc').value,
    categoryId: 1
  };
  api('/admin/equipment', { method: 'POST', body: JSON.stringify(body) })
    .then(() => { alert('Товар добавлен!'); loadCatalog(); })
    .catch(e => alert('Ошибка: ' + e.message));
}

function loadStats() {
  api('/admin/stats').then(d => {
    document.getElementById('statsResult').innerHTML = `
      <p>Всего броней: <b>${d.totalBookings}</b></p>
      <p>Выручка (завершенные): <b>${d.revenue} ₽</b></p>
      <p>Просрочено: <b>${d.overdue}</b></p>
      <p>По статусам: ${d.byStatus.map(s => s.status + ': ' + s.count).join(', ')}</p>
    `;
  }).catch(e => alert(e.message));
}

function loadAllBookings() {
  api('/bookings/all').then(list => {
    document.getElementById('allBookings').innerHTML = list.map(b => `
      <div class="booking-item">
        <div class="booking-info">
          <b>#${b.id}</b> Клиент: ${b.client}<br>
          <small>${b.startDate.slice(0,10)} — ${b.endDate.slice(0,10)}</small>
        </div>
        <div style="display:flex; align-items:center; gap: 12px;">
          <span class="booking-status status-${b.status}">${b.status}</span>
          <b>${b.totalPrice} ₽</b>
          ${b.status === 'pending' ? `<button class="btn-primary" onclick="confirmBooking(${b.id})">Подтвердить</button>` : ''}
          ${b.status === 'confirmed' ? `<button class="btn-primary" onclick="issueBooking(${b.id})">Выдать</button>` : ''}
          ${b.status === 'issued' ? `<button class="btn-primary" onclick="returnBooking(${b.id})">Принять</button>` : ''}
        </div>
      </div>`).join('');
  }).catch(() => {});
}

function confirmBooking(id) {
  api('/bookings/' + id + '/confirm', { method: 'PATCH' }).then(loadAllBookings).catch(e => alert(e.message));
}
function issueBooking(id) {
  api('/bookings/' + id + '/issue', { method: 'PATCH' }).then(loadAllBookings).catch(e => alert(e.message));
}
function returnBooking(id) {
  const fee = prompt('Введите сумму штрафа за повреждения (0, если нет):', '0');
  if (fee === null) return;
  api('/bookings/' + id + '/return', { method: 'PATCH', body: JSON.stringify({ damageFee: +fee, damageComment: 'Без комментария' }) })
    .then(loadAllBookings).catch(e => alert(e.message));
}

// Инициализация при загрузке страницы
if (token) {
  updateUI();
} else {
  loadCatalog();
}