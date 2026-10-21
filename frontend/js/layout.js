/* ============================================================
   LAYOUT - Sidebar + Topbar (Role-based, Multi-Tenant)
   ============================================================ */

var API_URL = (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1')
    ? 'http://localhost:5017/api'
    : '/api';
var TOKEN   = localStorage.getItem('token');
var ROLE    = localStorage.getItem('role') || '';
var DEALER_ID   = localStorage.getItem('dealerId');
var DEALER_NAME = localStorage.getItem('dealerName') || '';

(function () {
    var isLoginPage = window.location.pathname.indexOf('login.html') !== -1;
    if (!TOKEN && !isLoginPage) window.location.href = 'login.html';
})();

/* ===== MENU per ROLE ===== */
function getNavItems() {
    var all = [
        { href: 'dashboard.html',       label: 'Kendaraan',        icon: 'k-i-car',       key: 'dashboard', roles: ['SuperAdmin','DealerAdmin','ServiceAdvisor','Mechanic','Customer'] },
        { href: 'bookings.html',        label: 'Booking Servis',   icon: 'k-i-calendar',  key: 'bookings',  roles: ['SuperAdmin','DealerAdmin','ServiceAdvisor','Customer'] },
        { href: 'customers.html',       label: 'Customer',         icon: 'k-i-user',      key: 'customers', roles: ['SuperAdmin','DealerAdmin','ServiceAdvisor'] },
        { href: 'service-history.html', label: 'Riwayat Servis',   icon: 'k-i-clipboard', key: 'history',   roles: ['SuperAdmin','DealerAdmin','ServiceAdvisor','Mechanic','Customer'] },
        { href: 'invoices.html',        label: 'Invoice',          icon: 'k-i-dollar',    key: 'invoices',  roles: ['SuperAdmin','DealerAdmin','ServiceAdvisor'] },
        { href: 'users.html',           label: 'Kelola User',      icon: 'k-i-gear',      key: 'users',     roles: ['SuperAdmin','DealerAdmin'] },
        { href: 'reminders.html', label: 'Reminder Servis', icon: 'k-i-bell', key: 'reminders', roles: ['SuperAdmin','DealerAdmin','ServiceAdvisor'] },
        { href: 'analytics.html', label: 'Analytics', icon: 'k-i-chart-line', key: 'analytics', roles: ['SuperAdmin','DealerAdmin','ServiceAdvisor'] },
    ];
    return all.filter(function (i) { return i.roles.indexOf(ROLE) !== -1; });
}

/* ===== UTILITIES ===== */
function logout() {
    kendo.confirm('Yakin ingin keluar dari sistem?').then(function () {
        localStorage.clear();
        window.location.href = 'login.html';
    });
}

function apiCall(url, method, data, onSuccess, onError) {
    return $.ajax({
        url: url,
        method: method || 'GET',
        contentType: 'application/json',
        headers: { 'Authorization': 'Bearer ' + TOKEN },
        data: data ? JSON.stringify(data) : null
    })
    .done(function (res) { if (onSuccess) onSuccess(res); })
    .fail(function (xhr) {
        if (xhr.status === 401) {
            kendo.alert('Sesi Anda telah berakhir. Silakan login ulang.').then(function () {
                localStorage.clear();
                window.location.href = 'login.html';
            });
            return;
        }
        if (onError) onError(xhr);
        else showToast('error', (xhr.responseJSON && xhr.responseJSON.message) || xhr.statusText || 'Terjadi kesalahan');
    });
}

function formatRupiah(a) { return 'Rp ' + (a || 0).toLocaleString('id-ID'); }
function formatDate(d) { return d ? kendo.toString(kendo.parseDate(d), 'dd MMM yyyy') : '-'; }
function formatDateTime(d) { return d ? kendo.toString(kendo.parseDate(d), 'dd MMM yyyy HH:mm') : '-'; }

var _notification = null;
function showToast(type, message) {
    if (!_notification) {
        _notification = $('<div id="app-notification"></div>').kendoNotification({
            position: { pinned: true, top: 20, right: 20 },
            autoHideAfter: 4000,
            stacking: 'down',
            templates: [{
                type: 'success',
                template: '<div class="toast toast-success"><span class="k-icon k-i-check-circle"></span> #= message #</div>'
            }, {
                type: 'error',
                template: '<div class="toast toast-error"><span class="k-icon k-i-error"></span> #= message #</div>'
            }, {
                type: 'info',
                template: '<div class="toast toast-info"><span class="k-icon k-i-info-circle"></span> #= message #</div>'
            }, {
                type: 'warning',
                template: '<div class="toast toast-warning"><span class="k-icon k-i-warning"></span> #= message #</div>'
            }]
        }).data('kendoNotification');
    }
    _notification.show({ message: message }, type);
}

/* ===== SIDEBAR ===== */
function buildSidebar(activeKey) {
    var fullName = localStorage.getItem('fullName') || 'User';
    var initials = fullName.split(' ').filter(Boolean).map(function (w) { return w.charAt(0); }).slice(0, 2).join('').toUpperCase();

    var items = getNavItems();
    var navHtml = items.map(function (item) {
        var active = item.key === activeKey ? ' active' : '';
        return '<a href="' + item.href + '" class="nav-item' + active + '">' +
            '<span class="k-icon ' + item.icon + '"></span>' +
            '<span class="nav-label">' + item.label + '</span>' +
            '</a>';
    }).join('');

    // Info dealer di bawah user
    var dealerInfo = DEALER_NAME
        ? '<div class="user-dealer"><span class="k-icon k-i-marker-pin"></span> ' + DEALER_NAME + '</div>'
        : '<div class="user-dealer" style="color:#9ca3af;background:rgba(255,255,255,0.03);"><span class="k-icon k-i-globe"></span> Head Office</div>';

    return '' +
        '<aside class="sidebar">' +
            '<div class="sidebar-brand">' +
                '<div class="brand-mark">' +
                    '<span class="mark-diamond d1"></span>' +
                    '<span class="mark-diamond d2"></span>' +
                    '<span class="mark-diamond d3"></span>' +
                '</div>' +
                '<div class="brand-text">' +
                    '<div class="brand-name">AutoCare</div>' +
                    '<div class="brand-sub">Service Booking</div>' +
                '</div>' +
            '</div>' +
            '<nav class="sidebar-nav">' +
                '<div class="nav-section">Menu Utama</div>' +
                navHtml +
            '</nav>' +
            '<div class="sidebar-footer">' +
                '<div class="user-card">' +
                    '<div class="user-avatar">' + initials + '</div>' +
                    '<div class="user-meta">' +
                        '<div class="user-name">' + fullName + '</div>' +
                        '<div class="user-role">' + ROLE + '</div>' +
                    '</div>' +
                '</div>' +
                dealerInfo +
                '<button class="btn-logout" onclick="logout()">' +
                    '<span class="k-icon k-i-logout"></span> Keluar' +
                '</button>' +
            '</div>' +
        '</aside>';
}

/* ===== TOPBAR ===== */
function buildTopbar(title, subtitle) {
    return '' +
        '<header class="topbar">' +
            '<div class="topbar-left">' +
                '<h1 class="page-title">' + (title || '') + '</h1>' +
                (subtitle ? '<p class="page-subtitle">' + subtitle + '</p>' : '') +
            '</div>' +
            '<div class="topbar-right" id="topbarActions"></div>' +
        '</header>';
}

/* ===== MAIN ===== */
function renderLayout(activeKey, title, subtitle) {
    var contentHtml = $('#page-root').html();
    var layoutHtml = '' +
        '<div class="app-layout">' +
            buildSidebar(activeKey) +
            '<main class="main-area">' +
                buildTopbar(title, subtitle) +
                '<div class="content-area">' + contentHtml + '</div>' +
            '</main>' +
        '</div>';
    $('#page-root').replaceWith(layoutHtml);
    if (title) document.title = title + ' — AutoCare Service';
}

function setTopbarActions(html) {
    $('#topbarActions').html(html);
}

function defaultGridOptions() {
    return {
        height: 560,
        sortable: true,
        filterable: false,
        pageable: {
            refresh: true,
            pageSizes: [10, 25, 50, 100],
            buttonCount: 5,
            messages: { itemsPerPage: 'Baris per halaman' }
        },
        noRecords: {
            template: '<div class="empty-state"><span class="k-icon k-i-inbox"></span><p>Tidak ada data</p></div>'
        },
        resizable: true
    };
}