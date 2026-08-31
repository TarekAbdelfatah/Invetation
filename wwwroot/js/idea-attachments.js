(function () {
  'use strict';

  function getAntiForgeryToken() {
    const m = document.querySelector('input[name="__RequestVerificationToken"]');
    return m ? m.value : '';
  }

  function attach(scope) {
    scope = scope || document;
    const card = scope.querySelector('[data-idea-id]');
    if (!card) return;
    const ideaId = card.dataset.ideaId;
    const fileInput = card.querySelector('[data-attachments-input]');
    const uploadBtn = card.querySelector('[data-attachments-upload]');
    const list = card.querySelector('[data-attachments-list]');
    const status = card.querySelector('[data-attachments-status]');
    const progress = card.querySelector('[data-attachments-progress]');
    const bar = progress ? progress.querySelector('.progress-bar') : null;
    if (!fileInput || !uploadBtn || !list) return;

    function setStatus(msg, isError) {
      if (!status) return;
      status.textContent = msg;
      status.classList.toggle('text-danger', !!isError);
      status.classList.toggle('text-muted', !isError);
    }

    function renderList(items) {
      list.innerHTML = '';
      items.forEach(a => {
        const li = document.createElement('li');
        li.className = 'list-group-item d-flex justify-content-between align-items-center';
        const left = document.createElement('span');
        const sizeKb = Math.round((a.sizeBytes || 0) / 1024);
        left.textContent = `${a.fileName} (${sizeKb} ك.ب)`;
        const small = document.createElement('small');
        small.className = 'text-muted';
        small.textContent = new Date(a.uploadedAt).toLocaleString('ar-SA');
        li.appendChild(left);
        li.appendChild(small);
        list.appendChild(li);
      });
    }

    async function refresh() {
      try {
        const res = await fetch(`/api/Attachment/list?ideaId=${encodeURIComponent(ideaId)}`, {
          credentials: 'include',
          headers: { 'Accept': 'application/json' }
        });
        if (!res.ok) { setStatus('تعذّر تحميل المرفقات.', true); return; }
        const data = await res.json();
        renderList(data.items || []);
        const remaining = (data.maxCount || 2) - (data.items || []).length;
        setStatus(remaining > 0 ? `متبقي ${remaining} ملف.` : 'تم بلوغ الحد الأقصى.', false);
        fileInput.disabled = remaining <= 0;
      } catch (e) {
        setStatus('تعذّر الاتصال بالخادم.', true);
      }
    }

    function upload() {
      const file = fileInput.files && fileInput.files[0];
      if (!file) { setStatus('اختر ملفاً أولاً.', true); return; }
      const fd = new FormData();
      fd.append('ideaId', ideaId);
      fd.append('file', file);

      const xhr = new XMLHttpRequest();
      xhr.open('POST', '/api/Attachment/upload');
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

    uploadBtn.addEventListener('click', upload);
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
