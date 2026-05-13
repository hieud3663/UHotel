(() => {
    'use strict';

    const config = window.bookingCalendarConfig || {};
    const grid = document.getElementById('bookingCalendarGrid');
    const statusBox = document.getElementById('bookingCalendarStatus');
    const rangeTitle = document.getElementById('calendarRangeTitle');
    const form = document.getElementById('bookingCalendarFilterForm');
    const startInput = document.getElementById('calendarStartDate');
    const viewInput = document.getElementById('calendarViewMode');
    const resetButton = document.getElementById('resetCalendarFilters');
    const modalElement = document.getElementById('bookingEventModal');
    const modalBody = document.getElementById('bookingEventModalBody');
    const modalDetailsLink = document.getElementById('bookingEventDetailsLink');
    const modal = modalElement && window.bootstrap ? new bootstrap.Modal(modalElement) : null;
    const priceConfirmModalElement = document.getElementById('priceChangeConfirmModal');
    const priceConfirmModalBody = document.getElementById('priceChangeConfirmModalBody');
    const priceConfirmButton = document.getElementById('confirmPriceChangeButton');
    const cancelPriceButton = document.getElementById('cancelPriceChangeButton');
    const priceConfirmModal = priceConfirmModalElement && window.bootstrap
        ? new bootstrap.Modal(priceConfirmModalElement, { backdrop: 'static', keyboard: false })
        : null;

    const state = {
        startDate: parseLocalDate(config.startDate) || new Date(),
        viewMode: config.viewMode || 'week',
        rooms: [],
        events: [],
        tasks: [],
        days: [],
        draggingEventId: null
    };

    document.addEventListener('DOMContentLoaded', initialize);

    function initialize() {
        if (!grid) return;

        bindToolbar();
        loadCalendar();
    }

    function bindToolbar() {
        document.querySelectorAll('[data-calendar-nav]').forEach(button => {
            button.addEventListener('click', () => {
                const action = button.dataset.calendarNav;
                if (action === 'today') {
                    state.startDate = new Date();
                } else {
                    const step = action === 'next' ? 1 : -1;
                    state.startDate = addViewStep(state.startDate, state.viewMode, step);
                }
                syncHiddenInputs();
                loadCalendar();
            });
        });

        document.querySelectorAll('[data-view-mode]').forEach(button => {
            button.addEventListener('click', () => {
                state.viewMode = button.dataset.viewMode || 'week';
                syncHiddenInputs();
                updateViewButtons();
                loadCalendar();
            });
        });

        if (resetButton) {
            resetButton.addEventListener('click', () => {
                const url = new URL(window.location.href);
                url.search = '';
                window.location.href = url.toString();
            });
        }

        if (form) {
            form.addEventListener('submit', () => syncHiddenInputs());
        }
    }

    async function loadCalendar() {
        try {
            showLoading();
            clearStatus();
            state.days = buildDateRange(state.startDate, state.viewMode);
            updateRangeTitle();

            const params = buildCommonParams();
            const eventParams = new URLSearchParams(params);
            eventParams.set('start', toDateTimeLocal(state.days[0]));
            eventParams.set('end', toDateTimeLocal(addDays(state.days[state.days.length - 1], 1)));

            const [rooms, events, tasks] = await Promise.all([
                fetchJson(`${config.urls.rooms}?${params.toString()}`),
                fetchJson(`${config.urls.events}?${eventParams.toString()}`),
                fetchJson(`${config.urls.tasks}?${eventParams.toString()}`)
            ]);

            state.rooms = rooms || [];
            state.events = events || [];
            state.tasks = tasks || [];
            renderCalendar();
        } catch (error) {
            grid.innerHTML = `<div class="calendar-empty-state text-danger"><i class="fas fa-exclamation-triangle"></i> ${escapeHtml(error.message || 'Không thể tải lịch đặt phòng.')}</div>`;
        }
    }

    function buildCommonParams() {
        const params = new URLSearchParams();
        params.set('roomCategoryID', document.getElementById('roomCategoryID')?.value || config.roomCategoryID || '');
        params.set('roomStatus', document.getElementById('roomStatus')?.value || config.roomStatus || '');
        params.set('reservationStatus', document.getElementById('reservationStatus')?.value || config.reservationStatus || '');
        params.set('keyword', document.getElementById('keyword')?.value || config.keyword || '');
        return params;
    }

    function renderCalendar() {
        if (!state.rooms.length) {
            grid.innerHTML = '<div class="calendar-empty-state"><i class="fas fa-door-closed"></i> Không có phòng phù hợp với bộ lọc.</div>';
            return;
        }

        grid.style.setProperty('--calendar-days', state.days.length.toString());

        const header = `
            <div class="calendar-row calendar-header-row">
                <div class="calendar-corner">Phòng</div>
                ${state.days.map(day => `<div class="calendar-day-header ${isToday(day) ? 'today' : ''}">${formatDayHeader(day)}</div>`).join('')}
            </div>`;

        const rows = state.rooms.map(room => {
            const cells = state.days.map(day => renderDayCell(room, day)).join('');
            return `
                <div class="calendar-row">
                    <div class="calendar-room-cell">
                        <div class="room-title"><i class="fas fa-bed"></i> ${escapeHtml(room.title || room.id)}</div>
                        <div class="room-meta">
                            <span class="room-status-pill">${escapeHtml(room.roomStatusText || room.roomStatus || '')}</span>
                            ${room.hasOpenTask ? `<span class="room-task-pill">${escapeHtml(room.openTaskText || 'Có yêu cầu mở')}</span>` : ''}
                        </div>
                    </div>
                    ${cells}
                </div>`;
        }).join('');

        grid.innerHTML = header + rows;
        bindRenderedCells();
    }

    function renderDayCell(room, day) {
        const dateValue = toDateInput(day);
        const events = state.events.filter(event => event.roomID === room.id && intersectsDay(event.start, event.end, day));
        const tasks = state.tasks.filter(task => task.roomID === room.id && intersectsDay(task.start, task.end, day));
        const isPast = endOfDay(day) < new Date();

        return `
            <div class="calendar-day-cell ${isToday(day) ? 'today' : ''} ${isPast ? 'past-slot' : ''}"
                 data-room-id="${escapeAttribute(room.id)}"
                 data-date="${dateValue}">
                ${events.map(renderEvent).join('')}
                ${tasks.map(renderTask).join('')}
            </div>`;
    }

    function renderEvent(event) {
        const editable = Boolean(event.editable && config.canEditSchedule);
        const checkoutNote = event.checkoutTimingText
            ? `<div class="calendar-event-meta"><i class="fas fa-sign-out-alt"></i> ${escapeHtml(event.checkoutTimingText)}</div>`
            : '';

        return `
            <div class="calendar-event"
                 data-event-id="${escapeAttribute(event.id)}"
                 draggable="${editable ? 'true' : 'false'}"
                 style="background:${escapeAttribute(event.color || '#2563eb')}">
                <div class="calendar-event-title">${escapeHtml(event.reservationFormID)} - ${escapeHtml(event.customerName || 'Khách hàng')}</div>
                <div class="calendar-event-meta"><i class="fas fa-clock"></i> ${formatDateTime(event.start)} → ${formatDateTime(event.end)}</div>
                <div class="calendar-event-meta"><i class="fas fa-info-circle"></i> ${escapeHtml(event.statusText || '')}</div>
                ${checkoutNote}
                ${editable ? '<div class="calendar-event-actions"><button type="button" data-resize="minus"><i class="fas fa-compress-alt"></i> Giảm 1 ngày</button><button type="button" data-resize="plus"><i class="fas fa-expand-alt"></i> Tăng 1 ngày</button></div>' : ''}
            </div>`;
    }

    function renderTask(task) {
        const isMaintenance = task.taskType === 'MAINTENANCE';
        return `
            <div class="calendar-task ${isMaintenance ? 'maintenance' : ''}" data-task-url="${escapeAttribute(task.detailsUrl || '#')}">
                <div class="calendar-task-title"><i class="fas ${isMaintenance ? 'fa-tools' : 'fa-broom'}"></i> ${escapeHtml(task.title || 'Công việc phòng')}</div>
                <div class="calendar-task-meta">${escapeHtml(task.status || '')}</div>
            </div>`;
    }

    function bindRenderedCells() {
        grid.querySelectorAll('.calendar-day-cell').forEach(cell => {
            cell.addEventListener('click', handleSlotClick);
            cell.addEventListener('dragover', event => {
                if (!state.draggingEventId) return;
                event.preventDefault();
                cell.classList.add('drop-target');
            });
            cell.addEventListener('dragleave', () => cell.classList.remove('drop-target'));
            cell.addEventListener('drop', handleEventDrop);
        });

        grid.querySelectorAll('.calendar-event').forEach(element => {
            element.addEventListener('click', handleEventClick);
            element.addEventListener('dragstart', event => {
                if (element.getAttribute('draggable') !== 'true') {
                    event.preventDefault();
                    return;
                }
                state.draggingEventId = element.dataset.eventId;
                element.classList.add('dragging');
                event.dataTransfer.setData('text/plain', state.draggingEventId || '');
            });
            element.addEventListener('dragend', () => {
                state.draggingEventId = null;
                element.classList.remove('dragging');
                grid.querySelectorAll('.drop-target').forEach(cell => cell.classList.remove('drop-target'));
            });
        });

        grid.querySelectorAll('[data-resize]').forEach(button => {
            button.addEventListener('click', handleResizeClick);
        });

        grid.querySelectorAll('.calendar-task').forEach(element => {
            element.addEventListener('click', event => {
                event.stopPropagation();
                const url = element.dataset.taskUrl;
                if (url && url !== '#') window.location.href = url;
            });
        });
    }

    async function handleSlotClick(event) {
        if (event.target.closest('.calendar-event') || event.target.closest('.calendar-task')) return;

        const cell = event.currentTarget;
        const roomID = cell.dataset.roomId;
        const date = parseLocalDate(cell.dataset.date);
        const now = new Date();
        const checkInDate = new Date(date.getFullYear(), date.getMonth(), date.getDate(), Math.max(now.getHours() + 1, 14), 0, 0);
        const checkOutDate = addDays(checkInDate, 1);

        const available = await checkAvailability(roomID, checkInDate, checkOutDate, null);
        if (!available.available) {
            showStatus(available.message || 'Phòng không khả dụng.', 'warning');
            return;
        }

        const url = new URL(config.urls.reservationCreate, window.location.origin);
        url.searchParams.set('roomID', roomID);
        url.searchParams.set('checkInDate', toDateTimeLocal(checkInDate));
        url.searchParams.set('checkOutDate', toDateTimeLocal(checkOutDate));
        window.location.href = url.toString();
    }

    function handleEventClick(event) {
        const resizeButton = event.target.closest('[data-resize]');
        if (resizeButton) return;
        event.stopPropagation();

        const eventId = event.currentTarget.dataset.eventId;
        const item = state.events.find(e => e.id === eventId);
        if (!item) return;

        const checkoutTimingBadge = item.checkoutTimingText
            ? `<span class="checkout-timing-badge">${escapeHtml(item.checkoutTimingText)}</span>`
            : '';
        const actualCheckoutRow = item.actualCheckOutDate
            ? `<div><dt>Trả phòng thực tế</dt><dd>${formatDateTime(item.actualCheckOutDate)}</dd></div>`
            : '';

        modalBody.innerHTML = `
            <dl class="modal-detail-list">
                <div><dt>Mã phiếu</dt><dd>${escapeHtml(item.reservationFormID)}</dd></div>
                <div><dt>Khách hàng</dt><dd>${escapeHtml(item.customerName || 'Chưa có')}</dd></div>
                <div><dt>Phòng</dt><dd>${escapeHtml(item.roomID)}</dd></div>
                <div><dt>Loại phòng</dt><dd>${escapeHtml(item.roomCategoryName || 'Chưa có')}</dd></div>
                <div><dt>Nhận phòng</dt><dd>${formatDateTime(item.start)}</dd></div>
                <div><dt>Đến khi phòng bị chiếm</dt><dd>${formatDateTime(item.end)}</dd></div>
                <div><dt>Trả phòng dự kiến</dt><dd>${formatDateTime(item.expectedCheckOutDate || item.end)}</dd></div>
                ${actualCheckoutRow}
                <div><dt>Trạng thái</dt><dd>${escapeHtml(item.statusText || '')} ${checkoutTimingBadge}</dd></div>
            </dl>`;
        modalDetailsLink.href = item.detailsUrl || `${config.urls.reservationDetails}/${encodeURIComponent(item.reservationFormID)}`;
        if (modal) modal.show();
    }

    async function handleEventDrop(event) {
        event.preventDefault();
        const cell = event.currentTarget;
        cell.classList.remove('drop-target');

        const eventId = event.dataTransfer.getData('text/plain') || state.draggingEventId;
        const item = state.events.find(e => e.id === eventId);
        if (!item || !item.editable) return;

        const targetDate = parseLocalDate(cell.dataset.date);
        const originalStart = parseDateTime(item.start);
        const originalEnd = parseDateTime(item.end);
        const durationMs = originalEnd.getTime() - originalStart.getTime();
        const newStart = new Date(targetDate.getFullYear(), targetDate.getMonth(), targetDate.getDate(), originalStart.getHours(), originalStart.getMinutes(), 0);
        const newEnd = new Date(newStart.getTime() + durationMs);

        await moveReservation(item, cell.dataset.roomId, newStart, newEnd);
    }

    async function handleResizeClick(event) {
        event.stopPropagation();
        const button = event.currentTarget;
        const container = button.closest('.calendar-event');
        const item = state.events.find(e => e.id === container?.dataset.eventId);
        if (!item || !item.editable) return;

        const newEnd = parseDateTime(item.end);
        newEnd.setDate(newEnd.getDate() + (button.dataset.resize === 'plus' ? 1 : -1));
        const start = parseDateTime(item.start);
        if (newEnd <= start) {
            showStatus('Ngày trả phòng phải sau ngày nhận phòng.', 'warning');
            return;
        }

        await moveReservation(item, item.roomID, start, newEnd);
    }

    async function moveReservation(item, roomID, checkInDate, checkOutDate) {
        const available = await checkAvailability(roomID, checkInDate, checkOutDate, item.reservationFormID);
        if (!available.available) {
            showStatus(available.message || 'Không thể cập nhật do trùng lịch.', 'warning');
            return;
        }

        try {
            const pricePreview = await previewPriceChange(item.reservationFormID, roomID, checkInDate, checkOutDate);
            const confirmPriceChange = await confirmPricePreview(pricePreview);
            if (confirmPriceChange === null) {
                showStatus('Đã hủy cập nhật lịch đặt phòng.', 'warning');
                return;
            }

            const response = await fetch(config.urls.moveReservation, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgeryToken()
                },
                body: JSON.stringify({
                    reservationFormID: item.reservationFormID,
                    roomID,
                    checkInDate: toDateTimeLocal(checkInDate),
                    checkOutDate: toDateTimeLocal(checkOutDate),
                    confirmPriceChange
                })
            });

            const result = await response.json().catch(() => ({}));
            if (!response.ok || result.success === false) {
                if (result.requiresPriceConfirmation && result.pricePreview) {
                    throw new Error(buildPriceConfirmationText(result.pricePreview));
                }
                throw new Error(result.message || 'Không thể cập nhật lịch đặt phòng.');
            }

            showStatus(result.message || 'Đã cập nhật lịch đặt phòng.', 'success');
            await loadCalendar();
        } catch (error) {
            showStatus(error.message || 'Không thể cập nhật lịch đặt phòng.', 'error');
        }
    }

    async function checkAvailability(roomID, checkInDate, checkOutDate, excludeReservationFormID) {
        const params = new URLSearchParams({
            roomID,
            checkInDate: toDateTimeLocal(checkInDate),
            checkOutDate: toDateTimeLocal(checkOutDate)
        });
        if (excludeReservationFormID) params.set('excludeReservationFormID', excludeReservationFormID);
        return fetchJson(`${config.urls.checkAvailability}?${params.toString()}`);
    }

    async function previewPriceChange(reservationFormID, roomID, checkInDate, checkOutDate) {
        const params = new URLSearchParams({
            reservationFormID,
            roomID,
            checkInDate: toDateTimeLocal(checkInDate),
            checkOutDate: toDateTimeLocal(checkOutDate)
        });
        return fetchJson(`${config.urls.previewPriceChange}?${params.toString()}`);
    }

    async function confirmPricePreview(preview) {
        if (!preview || !preview.requiresPriceConfirmation) return false;

        if (!priceConfirmModal || !priceConfirmModalBody || !priceConfirmButton || !cancelPriceButton) {
            showStatus('Không thể mở hộp thoại xác nhận giá. Vui lòng tải lại trang và thử lại.', 'error');
            return null;
        }

        priceConfirmModalBody.innerHTML = buildPriceConfirmationHtml(preview);
        priceConfirmModal.show();

        return new Promise(resolve => {
            let resolved = false;

            const cleanup = () => {
                priceConfirmButton.removeEventListener('click', handleConfirm);
                cancelPriceButton.removeEventListener('click', handleCancel);
                priceConfirmModalElement.removeEventListener('hidden.bs.modal', handleHidden);
            };

            const finish = value => {
                if (resolved) return;
                resolved = true;
                cleanup();
                priceConfirmModal.hide();
                resolve(value);
            };

            const handleConfirm = () => finish(true);
            const handleCancel = () => finish(null);
            const handleHidden = () => finish(null);

            priceConfirmButton.addEventListener('click', handleConfirm);
            cancelPriceButton.addEventListener('click', handleCancel);
            priceConfirmModalElement.addEventListener('hidden.bs.modal', handleHidden);
        });
    }

    function buildPriceConfirmationHtml(preview) {
        const difference = Number(preview.estimatedDifference || 0);
        const differenceClass = difference > 0 ? 'increase' : difference < 0 ? 'decrease' : 'neutral';
        const differenceText = difference > 0
            ? `Tăng ${formatCurrency(difference)}`
            : difference < 0
                ? `Giảm ${formatCurrency(Math.abs(difference))}`
                : 'Không đổi';

        return `
            <div class="price-confirm-summary">
                <div class="price-confirm-alert">
                    <i class="fas fa-exclamation-triangle mt-1"></i>
                    <div>${escapeHtml(preview.message || 'Giá phòng sẽ thay đổi khi chuyển lịch. Vui lòng kiểm tra trước khi xác nhận.')}</div>
                </div>
                <div class="price-confirm-grid">
                    <div class="price-confirm-card">
                        <h6><i class="fas fa-door-closed"></i> Thông tin hiện tại</h6>
                        ${priceRow('Phòng', preview.currentRoomID || 'Chưa có')}
                        ${priceRow('Loại phòng', preview.currentRoomCategoryName || 'Chưa có')}
                        ${priceRow('Đơn giá', `${formatCurrency(preview.currentUnitPrice)} / ${priceUnitText(preview.priceUnit)}`)}
                        ${priceRow('Tiền phòng dự kiến', formatCurrency(preview.currentEstimatedRoomCharge))}
                        ${priceRow('Tiền cọc hiện tại', formatCurrency(preview.currentDeposit))}
                    </div>
                    <div class="price-confirm-card">
                        <h6><i class="fas fa-door-open"></i> Sau khi chuyển lịch</h6>
                        ${priceRow('Phòng', preview.newRoomID || 'Chưa có')}
                        ${priceRow('Loại phòng', preview.newRoomCategoryName || 'Chưa có')}
                        ${priceRow('Đơn giá', `${formatCurrency(preview.newUnitPrice)} / ${priceUnitText(preview.priceUnit)}`)}
                        ${priceRow('Tiền phòng dự kiến', formatCurrency(preview.newEstimatedRoomCharge))}
                        ${priceRow('Tiền cọc đề xuất', formatCurrency(preview.suggestedDeposit))}
                    </div>
                </div>
                <div class="price-confirm-card">
                    <div class="price-confirm-row">
                        <span class="price-confirm-label">Chênh lệch dự kiến</span>
                        <span class="price-difference ${differenceClass}">${escapeHtml(differenceText)}</span>
                    </div>
                </div>
            </div>`;
    }

    function priceRow(label, value) {
        return `
            <div class="price-confirm-row">
                <span class="price-confirm-label">${escapeHtml(label)}</span>
                <span class="price-confirm-value">${escapeHtml(value)}</span>
            </div>`;
    }

    function buildPriceConfirmationText(preview) {
        const difference = Number(preview.estimatedDifference || 0);
        const differenceText = difference >= 0 ? `Tăng ${formatCurrency(difference)}` : `Giảm ${formatCurrency(Math.abs(difference))}`;
        return [
            'Đơn giá phòng sẽ thay đổi khi chuyển lịch:',
            '',
            `Phòng/loại phòng cũ: ${preview.currentRoomID || ''} - ${preview.currentRoomCategoryName || 'Chưa có'}`,
            `Phòng/loại phòng mới: ${preview.newRoomID || ''} - ${preview.newRoomCategoryName || 'Chưa có'}`,
            `Đơn giá cũ: ${formatCurrency(preview.currentUnitPrice)} / ${priceUnitText(preview.priceUnit)}`,
            `Đơn giá mới: ${formatCurrency(preview.newUnitPrice)} / ${priceUnitText(preview.priceUnit)}`,
            `Tiền phòng dự kiến cũ: ${formatCurrency(preview.currentEstimatedRoomCharge)}`,
            `Tiền phòng dự kiến mới: ${formatCurrency(preview.newEstimatedRoomCharge)}`,
            `Chênh lệch dự kiến: ${differenceText}`,
            `Tiền cọc hiện tại: ${formatCurrency(preview.currentDeposit)}`,
            `Tiền cọc đề xuất mới: ${formatCurrency(preview.suggestedDeposit)}`,
            '',
            'Xác nhận cập nhật lịch và áp dụng giá mới?'
        ].join('\n');
    }

    function priceUnitText(priceUnit) {
        return priceUnit === 'HOUR' ? 'giờ' : 'ngày';
    }

    function formatCurrency(value) {
        return Number(value || 0).toLocaleString('vi-VN') + ' VNĐ';
    }

    async function fetchJson(url, options) {
        const response = await fetch(url, options);
        if (!response.ok) {
            const body = await response.json().catch(() => null);
            throw new Error(body?.message || 'Yêu cầu không thành công.');
        }
        return response.json();
    }

    function showLoading() {
        grid.innerHTML = '<div class="calendar-loading"><i class="fas fa-spinner fa-spin"></i> Đang tải lịch đặt phòng...</div>';
    }

    function showStatus(message, type) {
        if (!statusBox) return;
        statusBox.className = `calendar-status ${type || 'success'}`;
        statusBox.textContent = message;
        statusBox.classList.remove('d-none');
    }

    function clearStatus() {
        if (!statusBox) return;
        statusBox.classList.add('d-none');
        statusBox.textContent = '';
    }

    function updateRangeTitle() {
        if (!rangeTitle || !state.days.length) return;
        const first = state.days[0];
        const last = state.days[state.days.length - 1];
        rangeTitle.textContent = first.toDateString() === last.toDateString()
            ? formatDayHeader(first)
            : `${formatShortDate(first)} - ${formatShortDate(last)}`;
    }

    function updateViewButtons() {
        document.querySelectorAll('[data-view-mode]').forEach(button => {
            const active = button.dataset.viewMode === state.viewMode;
            button.classList.toggle('btn-primary', active);
            button.classList.toggle('btn-outline-primary', !active);
        });
    }

    function syncHiddenInputs() {
        if (startInput) startInput.value = toDateInput(state.startDate);
        if (viewInput) viewInput.value = state.viewMode;
    }

    function buildDateRange(startDate, viewMode) {
        const normalized = new Date(startDate.getFullYear(), startDate.getMonth(), startDate.getDate());
        if (viewMode === 'day') return [normalized];
        if (viewMode === 'month') {
            const days = [];
            const first = new Date(normalized.getFullYear(), normalized.getMonth(), 1);
            const last = new Date(normalized.getFullYear(), normalized.getMonth() + 1, 0);
            for (let day = new Date(first); day <= last; day = addDays(day, 1)) {
                days.push(new Date(day));
            }
            return days;
        }

        const mondayOffset = (normalized.getDay() + 6) % 7;
        const monday = addDays(normalized, -mondayOffset);
        return Array.from({ length: 7 }, (_, index) => addDays(monday, index));
    }

    function addViewStep(date, viewMode, step) {
        if (viewMode === 'day') return addDays(date, step);
        if (viewMode === 'month') return new Date(date.getFullYear(), date.getMonth() + step, 1);
        return addDays(date, step * 7);
    }

    function intersectsDay(start, end, day) {
        const eventStart = parseDateTime(start);
        const eventEnd = parseDateTime(end);
        const dayStart = new Date(day.getFullYear(), day.getMonth(), day.getDate());
        const dayEnd = addDays(dayStart, 1);
        return eventStart < dayEnd && eventEnd > dayStart;
    }

    function addDays(date, days) {
        const result = new Date(date);
        result.setDate(result.getDate() + days);
        return result;
    }

    function endOfDay(date) {
        return new Date(date.getFullYear(), date.getMonth(), date.getDate(), 23, 59, 59);
    }

    function isToday(date) {
        const today = new Date();
        return date.getFullYear() === today.getFullYear() && date.getMonth() === today.getMonth() && date.getDate() === today.getDate();
    }

    function parseLocalDate(value) {
        if (!value) return null;
        const [datePart] = value.split('T');
        const parts = datePart.split('-').map(Number);
        if (parts.length !== 3 || parts.some(Number.isNaN)) return null;
        return new Date(parts[0], parts[1] - 1, parts[2]);
    }

    function parseDateTime(value) {
        return new Date(value);
    }

    function toDateInput(date) {
        return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
    }

    function toDateTimeLocal(date) {
        return `${toDateInput(date)}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
    }

    function formatDayHeader(date) {
        return date.toLocaleDateString('vi-VN', { weekday: 'short', day: '2-digit', month: '2-digit' });
    }

    function formatShortDate(date) {
        return date.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
    }

    function formatDateTime(value) {
        return parseDateTime(value).toLocaleString('vi-VN', {
            hour: '2-digit',
            minute: '2-digit',
            day: '2-digit',
            month: '2-digit',
            year: 'numeric'
        });
    }

    function pad(value) {
        return value.toString().padStart(2, '0');
    }

    function escapeHtml(value) {
        return String(value ?? '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function escapeAttribute(value) {
        return escapeHtml(value).replace(/`/g, '&#096;');
    }

    function getAntiForgeryToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    }
})();
