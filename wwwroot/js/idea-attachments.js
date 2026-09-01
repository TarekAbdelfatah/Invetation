(function () {
  'use strict';

  let confirmResolver = null;
  let confirmModal = null;

  function getAntiForgeryToken() {
    const m = document.querySelector('input[name="__RequestVerificationToken"]');
    return m ? m.value : '';
  }

  function attach(scope) {
    scope = scope || document;
    const card = scope.querySelector('[data-idea-id]');
    if (!card) return;
    const ideaId = card.dataset.ideaId;
    const uploadEndpoint = card.dataset.uploadEndpoint || '/api/Attachment/upload';
    const listEndpoint = card.dataset.listEndpoint || `/api/Attachment/list?ideaId=${encodeURIComponent(ideaId)}`;
    const uploadField = card.dataset.uploadField || 'ideaId';
    const isDraft = String(card.dataset.draftMode || '').toLowerCase() === 'true';
    // The list endpoint targets the saved idea in DB whenever it's available,
    // even when the idea is still an unsaved draft (no reference number yet).
    const hasRealIdea = listEndpoint && /\/api\/Attachment\/list(\?|$)/.test(listEndpoint);
    const isReadOnly = String(card.dataset.readOnly || '').toLowerCase() === 'true';
    const fileInput = card.querySelector('[data-attachments-input]');
    const uploadBtn = card.querySelector('[data-attachments-upload]');
    const list = card.querySelector('[data-attachments-list]');
    const status = card.querySelector('[data-attachments-status]');
    const progress = card.querySelector('[data-attachments-progress]');
    const bar = progress ? progress.querySelector('.progress-bar') : null;
    if (!list) return;
    if (isReadOnly && fileInput) { fileInput.disabled = true; }
    if (isReadOnly && uploadBtn) { uploadBtn.disabled = true; uploadBtn.classList.add('d-none'); }
    if (isReadOnly && status) { status.classList.add('d-none'); }

    function setStatus(msg, isError) {
      if (!status) return;
      status.textContent = msg;
      status.classList.toggle('text-danger', !!isError);
      status.classList.toggle('text-muted', !isError);
    }

    function downloadUrlFor(a) {
      if (hasRealIdea) {
        return `/Attachment/Download?attachmentId=${encodeURIComponent(a.id)}`;
      }
      if (isDraft) {
        return `/api/Attachment/downloadDraft?draftId=${encodeURIComponent(ideaId)}&fileName=${encodeURIComponent(a.fileName)}`;
      }
      return `/Attachment/Download?attachmentId=${encodeURIComponent(a.id)}`;
    }

    function renderList(items) {
      list.innerHTML = '';
      if (!items || items.length === 0) {
        const li = document.createElement('li');
        li.className = 'list-group-item text-muted text-center small py-3';
        li.textContent = 'لا توجد مرفقات.';
        list.appendChild(li);
        return;
      }
      items.forEach(a => {
        const li = document.createElement('li');
        li.className = 'list-group-item d-flex flex-wrap justify-content-between align-items-center gap-2';

        const left = document.createElement('span');
        left.className = 'd-inline-flex align-items-center gap-2';
        const icon = document.createElement('span');
        icon.className = 'material-icons text-danger';
        icon.style.fontSize = '18px';
        icon.textContent = 'picture_as_pdf';
        left.appendChild(icon);
        const name = document.createTextNode(`${a.fileName} (${Math.round((a.sizeBytes || 0) / 1024)} ك.ب)`);
        left.appendChild(name);
        li.appendChild(left);

        const right = document.createElement('span');
        right.className = 'd-inline-flex align-items-center gap-2';
        const small = document.createElement('small');
        small.className = 'text-muted';
        small.textContent = a.uploadedAt ? new Date(a.uploadedAt).toLocaleString('ar-SA') : '';
        right.appendChild(small);

        if (!isReadOnly) {
          const del = document.createElement('button');
          del.type = 'button';
          del.className = 'btn btn-sm btn-outline-danger';
          del.setAttribute('data-attachment-id', a.id);
          del.setAttribute('data-attachment-name', a.fileName);
          del.innerHTML = '<span class="material-icons align-middle" style="font-size:16px">delete</span> حذف';
          del.addEventListener('click', function () { deleteAttachment(a, li); });
          right.appendChild(del);
        }

        const download = document.createElement('a');
        download.className = 'btn btn-sm btn-outline-primary';
        download.href = downloadUrlFor(a);
        download.setAttribute('download', a.fileName);
        download.setAttribute('target', '_blank');
        download.rel = 'noopener noreferrer';
        download.innerHTML = '<span class="material-icons align-middle" style="font-size:16px">download</span> تحميل';
        right.appendChild(download);
        li.appendChild(right);

        list.appendChild(li);
      });
    }

    function ensureConfirmModal() {
      let modal = document.getElementById('confirm-modal');
      if (!modal) {
        const tpl = document.createElement('div');
        tpl.innerHTML = `
<div id="confirm-modal" class="bog-modal" hidden role="dialog" aria-modal="true"
     aria-labelledby="confirm-modal-title" aria-describedby="confirm-modal-message">
    <div class="bog-modal-backdrop" data-confirm-cancel></div>
    <div class="bog-modal-dialog bog-modal-dialog-centered" role="document">
        <div class="bog-modal-content">
            <h5 id="confirm-modal-title" class="bog-modal-title">تأكيد الإجراء</h5>
            <p id="confirm-modal-message" class="bog-modal-body" data-confirm-message>هل أنت متأكد؟</p>
            <div class="bog-modal-actions">
                <button type="button" class="btn btn-outline-secondary" data-confirm-cancel>إلغاء</button>
                <button type="button" class="btn btn-danger" data-confirm-ok>تأكيد</button>
            </div>
        </div>
    </div>
</div>`;
        modal = tpl.firstElementChild;
        document.body.appendChild(modal);
      }
      bindStandaloneModal(modal);
      confirmModal = modal;
      return modal;
    }

    function bindStandaloneModal(modal) {
      if (modal.__bogBound) return;
      modal.__bogBound = true;

      const messageEl = modal.querySelector('[data-confirm-message]');
      const okBtn = modal.querySelector('[data-confirm-ok]');
      const cancelBtns = modal.querySelectorAll('[data-confirm-cancel]');

      window.__bogConfirm = function (msg, okLabel) {
        if (messageEl) messageEl.textContent = msg;
        if (okLabel) okBtn.textContent = okLabel;
        modal.hidden = false;
        modal.setAttribute('data-open', 'true');
        okBtn.focus();
        return new Promise((res) => { confirmResolver = res; });
      };

      function close(result) {
        modal.hidden = true;
        modal.removeAttribute('data-open');
        if (confirmResolver) {
          const r = confirmResolver;
          confirmResolver = null;
          r(result);
        }
      }

      okBtn.addEventListener('click', () => close(true));
      cancelBtns.forEach((b) => b.addEventListener('click', () => close(false)));
      document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && !modal.hidden) close(false);
      });
    }

    async function deleteAttachment(item, li) {
      if (isReadOnly) return;
      const modal = ensureConfirmModal();
      const confirmed = await window.__bogConfirm(`هل تريد حذف "${item.fileName}" نهائياً؟`, 'حذف');
      if (!confirmed) return;
      const token = getAntiForgeryToken();
      try {
        const res = await fetch(`/api/Attachment/delete/${encodeURIComponent(item.id)}`, {
          method: 'POST',
          credentials: 'include',
          headers: token ? { 'RequestVerificationToken': token } : {}
        });
        if (!res.ok) {
          let msg = 'فشل الحذف.';
          try { msg = (await res.json()).error || msg; } catch (_) {}
          await window.__bogConfirm(msg, 'حسناً');
          return;
        }
        li.remove();
        refresh();
      } catch (e) {
        await window.__bogConfirm('فشل الاتصال بالخادم.', 'حسناً');
      }
    }

    async function refresh() {
      try {
        const res = await fetch(listEndpoint, {
          credentials: 'include',
          headers: { 'Accept': 'application/json' }
        });
        if (!res.ok) { setStatus('تعذّر تحميل المرفقات.', true); return; }
        const data = await res.json();
        renderList(data.items || []);
        const remaining = (data.maxCount || 2) - (data.items || []).length;
        if (!isReadOnly) {
          setStatus(remaining > 0 ? `متبقي ${remaining} ملف.` : 'تم بلوغ الحد الأقصى.', false);
          if (fileInput) fileInput.disabled = remaining <= 0;
        }
      } catch (e) {
        setStatus('تعذّر الاتصال بالخادم.', true);
      }
    }

    function upload() {
      if (isReadOnly) { setStatus('القراءة فقط — لا يمكن إضافة مرفقات هنا.', true); return; }
      const file = fileInput.files && fileInput.files[0];
      if (!file) { setStatus('اختر ملفاً أولاً.', true); return; }
      const fd = new FormData();
      fd.append(uploadField, ideaId);
      fd.append('file', file);

      const xhr = new XMLHttpRequest();
      xhr.open('POST', uploadEndpoint);
      xhr.withCredentials = true;
      const token = getAntiForgeryToken();
      if (token) xhr.setRequestHeader('RequestVerificationToken', token);

      if (progress) progress.classList.remove('d-none');

      xhr.upload.onprogress = function (e) {
        if (e.lengthComputable && bar) {
          const pct = Math.round((e.loaded / e.total) * 100);
          bar.style.width = pct + '%';
          bar.textContent = pct + '%';
        }
      };
      xhr.onload = function () {
        if (progress) progress.classList.add('d-none');
        if (bar) { bar.style.width = '0%'; bar.textContent = ''; }
        if (xhr.status >= 200 && xhr.status < 300) {
          setStatus('تم رفع الملف بنجاح.', false);
          fileInput.value = '';
          refresh();
        } else {
          let msg = 'فشل الرفع.';
          try { msg = (JSON.parse(xhr.responseText).error) || msg; } catch (_) {}
          setStatus(msg, true);
        }
      };
      xhr.onerror = function () {
        if (progress) progress.classList.add('d-none');
        setStatus('فشل الاتصال.', true);
      };
      xhr.send(fd);
    }

    if (uploadBtn && !isReadOnly) {
      uploadBtn.addEventListener('click', upload);
    }
    refresh();
  }

  function bootstrap() {
    document.querySelectorAll('[data-idea-id]').forEach(el => {
      if (!el.__attachmentUploaderAttached) {
        el.__attachmentUploaderAttached = true;
        attach(el.parentElement || document);
      }
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', bootstrap);
  } else {
    bootstrap();
  }
})();
